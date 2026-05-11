# Daily Quests v1 — security & integrity audit

Walks every path through the daily-quests system that mutates state,
identifies exploits, and records mitigation status.

## Surface map

```
Player action  ─►  hook site                             ─►  fso_action_log + fso_daily_quests
─────────────      ──────────                                ────────────────────────────────
milestone gift  ─►  BirthdayGiftTask.Run                 ─►  MoneyEarned  +EARN progress
                    via Avatars.CreditBudgetAndRecord

starter cash    ─►  newAvatar.budget = N (NOT recorded)  ─►  (not logged)

lot visit       ─►  LotHost.RecordStartVisit             ─►  LotVisited   +VISIT progress
                    (filtered: visitorType == visitor)

catalog buy     ─►  LotServerGlobalLink.RegisterNewObject ─►  CatalogBought +BUY progress
                    (filtered: owner.HasValue && price > 0)

Cron roll       ─►  RollDailyQuestsTask                  ─►  fso_daily_quests INSERT
                                                            (LEFT JOIN guard — no double-roll)

Cron payout     ─►  RollDailyQuestsTask                  ─►  Avatars.CreditBudget (plain!)
                                                            MarkPaid

Manual claim    ─►  POST /userapi/quests/claim/{slot}    ─►  same as cron payout
                    (no auth in v1)

API read        ─►  GET /userapi/quests/today            ─►  read-only
                    (no auth in v1)
```

## Per-quest exploit analysis

### EARN — Earn §X today

| Aspect | Verdict |
|---|---|
| Hook site | `Avatars.CreditBudgetAndRecord` — called only from BirthdayGiftTask in v1 |
| Coverage | **Weak** — most player income (job rewards, peer transfers, refunds) flows through `Avatars.Transaction`, NOT `CreditBudget`. EARN quest will rarely complete via normal gameplay in v1. |
| Exploit | **None** — birthday gift award is one-shot per milestone per avatar, gated by `fso_events.GenericAvaTryParticipate`. Not repeatable. |
| Acceptable for v1? | Yes — broken-but-safe is preferable to broken-and-exploitable. EARN completion is rare in v1; document and ship. |
| Phase 1.5+ fix | Hook `Avatars.Transaction` for `source == uint.MaxValue` (system→player) flows with a filtered reason-code allow-list. Carefully exclude sell-back refunds (see BUY exploit below). |

### VISIT — Visit N unique lots today

| Aspect | Verdict |
|---|---|
| Hook site | `LotHost.RecordStartVisit` after `LotVisits.Visit` returns a valid id |
| Filter | `DbLotVisitorType.visitor` only — owner/roommate arrivals at their own lot don't qualify |
| Idempotency | `RecordActionIdempotent` checks `fso_action_log.ExistsToday(avatar, day, LotVisited, lot_id)` before bumping progress. Re-visiting the same lot does not double-count. |
| Exploit — multi-account farming | A player with N alts can mutually visit each other's lots. Bounded by reward cap (§reward × 1 per account per day). Same surface as any per-account daily reward; not a quests-specific issue. |
| Acceptable for v1? | Yes — clean. |

### BUY — Spend §X at the catalog today

| Aspect | Verdict |
|---|---|
| Hook site | `LotServerGlobalLink.RegisterNewObject` after `Objects.Create` succeeds |
| Filter | `owner.HasValue && dbo.value > 0` — owner-set and non-zero price |
| Trigger volume | Fires every catalog purchase. SimAntics already enforced payment via `PerformTransaction` before getting here. |
| Exploit — buy-sell cycle | **REAL.** Buy object §1000 → BUY progress +§1000. Delete (sell back) → refund hits via `Avatars.Transaction(uint.MaxValue, caller, refundAmount, 0)`. If sell-back recovery is ≥95% of price (TSO default for un-depreciated objects), attacker can churn a small object many times to complete the daily BUY quest with near-zero net cost. Cap on per-day exposure: §reward (max §3000). |
| Severity | Low-medium. Bounded daily by quest reward cap. Requires manual button-mashing — no automation in the vanilla client. |
| Mitigation in this commit | **Per-purchase contribution cap.** A single catalog purchase contributes at most §500 toward BUY quest progress regardless of actual price. Completing the daily §3800 target now requires ≥8 distinct purchases. Makes the sell-back cycle marginally tedious but not impossible. |
| Full fix (deferred) | **Net-spend tracking** via a new `IVMTSOGlobalLink.RecordSellBack(avatar, refund)` hook called from `VMNetDeleteObjectCmd`, plus `IDailyQuests.DecrementProgress(...)`. Subtract refund amount from open BUY progress; floor at 0. Buy-sell cycles would net 0 progress. Defer because it touches IVMTSOGlobalLink (shared interface, two no-op stubs needed) — larger surface than a v1 polish pass warrants. |
| Acceptable for v1 (with cap)? | Yes — bounded daily reward + tedium reduces exploit value to "not worth doing." |

## API surface

| Endpoint | Method | Auth | Reads / writes | Concern | Verdict |
|---|---|---|---|---|---|
| `/userapi/quests/today` | GET | none (v1) | read | leaks "what quests is avatar N doing today"? Not sensitive. | Safe. |
| `/userapi/quests/claim/{slot}` | POST | none (v1) | writes: CreditBudget + MarkPaid | credits the **target** avatar (URL param), not the requester. Worst-case griefer pays a stranger their own daily reward early. **No value extraction.** | Safe in v1. |

Real auth (cookie or token) lands in Phase 2 when the client gets a
session-scoped ApiClient. Documented in the design doc.

## Cron-task analysis

`RollDailyQuestsTask` is server-side, runs as freeso-server, no external
input.

| Concern | Status |
|---|---|
| Double-roll on cron re-run | Guarded by `LEFT JOIN fso_daily_quests` in `GetAvatarsNeedingRoll` — already-rolled avatars are excluded. Safe. |
| Double-pay on cron re-run | Guarded by `paid_ts IS NULL` filter in `GetUnpaidForDay` + `MarkPaid` only sets when `paid_ts IS NULL`. Safe. |
| Quest reward feeds back into EARN | `CreditBudget` (plain) used at payout, NOT `CreditBudgetAndRecord`. No feedback loop. Safe. |
| Action log unbounded growth | `Purge(today - 30)` at end of every run. Capped at ~30 days of rows. Safe. |
| Mail dispatch | Same pattern as existing BirthdayGiftTask. Safe. |

## Schema integrity

| Check | Status |
|---|---|
| Composite PK on `fso_daily_quests (avatar_id, day, slot)` | Prevents duplicate inserts at the DB layer too. |
| `fso_action_log` is append-only | No UPDATE/DELETE paths in code except cron purge. |
| `LEAST(target, progress + delta)` cap | Prevents over-completion. |
| `completed_ts IS NULL` filter on UPDATEs | Prevents re-completing an already-completed quest. |

## Mitigations applied in this audit pass

1. **BUY per-purchase contribution cap** — `LotServerGlobalLink.RegisterNewObject` now records `min(price, 500)` toward BUY quest progress rather than the full price. Reduces sell-back cycle value: instead of 4 cycles to clear a §3800 target, an attacker needs ≥8 distinct purchases. Combined with sell-back depreciation, the exploit becomes time-expensive enough to deter most abuse. Full net-spend tracking is the proper fix; deferred (see BUY section above).

## Known v1 limitations (deferred)

1. EARN quest rarely completes via gameplay — only triggers on milestone gifts. Phase 1.5: hook `Avatars.Transaction` with a curated reason-code allow-list.
2. SKILL quest type omitted from pool — needs live SimAntics → userApi pipe. Phase 1.5.
3. API endpoints lack auth — accepted because exploit surface is null (claim credits target, not requester). Phase 2.
4. Multi-account farming is possible for VISIT — same as any per-account daily-reward system. Accepted.

## Sign-off

System is acceptable to ship as v1 with the BUY sell-back mitigation
applied below. Remaining gaps are documented and have planned phases.