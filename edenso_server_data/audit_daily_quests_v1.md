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
| Hook sites | `Avatars.CreditBudgetAndRecord` (milestone gifts) + `Avatars.Transaction` (all system→avatar credits, **phase 1.5**) |
| Coverage | **Now broad.** Every system payout (SimAntics job rewards, bonus payouts, event prizes, lot refunds) bumps EARN progress. |
| Filter on Transaction hook | `success && amount > 0 && source_id == uint.MaxValue && !dstObj` — system source only, avatar dest only. Peer transfers EXCLUDED. |
| Why exclude peer transfers | Two players could ping money back and forth to clear each other's EARN quests with zero net cost. By excluding source=avatar, this exploit closes. |
| Known leak | Object sell-back refunds also flow source=MAX → avatar, so they DO count toward EARN. Combined with the BUY cap (§500/purchase), the buy→sell→buy loop nets at most a few hundred simoleons advantage over normal play. Bounded daily by quest reward caps (EARN §5000 + BUY §3000 = §8000 total). |
| Acceptable for v1.5? | Yes — covers normal gameplay, exploit value is small and time-expensive. |
| Future improvement (phase 2+) | Net-spend tracking via VMNetDeleteObjectCmd hook to neutralize sell-back leak entirely. |

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
| `/userapi/quests/claim/{slot}` | POST | none (v1) | writes: MarkPaid + CreditBudget (in that order, atomic) | credits the **target** avatar (URL param), not the requester. Worst-case griefer pays a stranger their own daily reward early. **No value extraction.** | Safe. |

### TOCTOU race fix on Claim (caught in phase 1.5 review)

The first cut of the Claim endpoint was vulnerable to a classic
time-of-check-to-time-of-use race:

```
T1: GET quest, paid_ts IS NULL → pass
T2: GET quest, paid_ts IS NULL → pass    ← T2 reads before T1 marks
T1: CreditBudget(+reward)
T2: CreditBudget(+reward)                 ← DOUBLE CREDIT
T1: MarkPaid (UPDATE … WHERE paid_ts IS NULL) → rows=1
T2: MarkPaid → rows=0 but already credited
```

Two concurrent claims would both pass the in-memory `paid_ts.HasValue`
check, both call `CreditBudget`, only one would successfully `MarkPaid`
— but the second credit had already fired. Exploit value: up to
`3 quests × max reward §5000 = §15,000` extra per day per script-capable
player.

**Fix applied:**

1. `IDailyQuests.MarkPaid` now returns rows affected (`int` not `void`).
2. Claim endpoint reorders to **MarkPaid first, CreditBudget only if
   MarkPaid returned 1**. The MariaDB row lock during UPDATE serializes
   concurrent calls; only the first observer of `paid_ts IS NULL`
   succeeds, the others get `rows=0` and a `410 Gone` response.
3. Same reorder applied in `RollDailyQuestsTask.Run` so the cron
   payout pass and a user manually claiming can't double-credit each
   other if they overlap on the same row.

### Race / concurrency on the other write paths

| Path | Race-safe? | How |
|---|---|---|
| `IncrementProgress` (action hooks) | Yes | Single-statement UPDATE with `LEAST(target, progress + delta)` and `WHERE completed_ts IS NULL`. MariaDB row lock serializes concurrent UPDATEs; increments accumulate correctly. |
| `RecordAction` (log insert) | Yes | Append-only INSERT; PK auto-increment. No conflict possible. |
| `RecordActionIdempotent` | Yes | ExistsToday + INSERT + UPDATE without an outer transaction. Two simultaneous first-time visits could each pass ExistsToday and double-insert. Mitigation: the action_log is purely audit; the side-effect (quest progress UPDATE) is bounded by `LEAST(target, progress + 1)`. Worst case: two log rows for one visit, one extra +1 quest progress. The quest still caps at target. Minor double-counting, no money extracted. Accept. |
| Roll task vs another roll task | Yes | `GetAvatarsNeedingRoll` LEFT JOIN against today's quest rows. Only one tick will see them as missing; the other sees them as inserted. |
| Transaction hook | Yes | Inline UPDATE inside the same connection as the transaction commit. MariaDB row lock again. |

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

2. **EARN hook on `Avatars.Transaction`** (phase 1.5) — bumps EARN progress when money flows from `uint.MaxValue` (the bank) to an avatar destination. Catches job rewards / bonuses / event prizes / lot refunds — i.e. genuinely earned simoleons via gameplay. Peer transfers (avatar → avatar) are deliberately excluded to prevent collusion farming.

## Known v1 limitations (deferred)

1. ~~EARN quest rarely completes via gameplay — only triggers on milestone gifts.~~ **Fixed in phase 1.5** — Transaction hook now catches system→avatar credits.
2. SKILL quest type omitted from pool — needs live SimAntics → userApi pipe. Phase 1.5+.
3. API endpoints lack auth — accepted because exploit surface is null (claim credits target, not requester). Phase 2.
4. Multi-account farming is possible for VISIT — same as any per-account daily-reward system. Accepted.
5. Sell-back leak into EARN — buy/sell-back cycle bumps EARN slightly. Phase 2 fix: net-spend tracking via VMNetDeleteObjectCmd refund hook.

## Sign-off

System is acceptable to ship as v1 with the BUY sell-back mitigation
applied below. Remaining gaps are documented and have planned phases.