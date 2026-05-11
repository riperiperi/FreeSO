# EdenSO Daily Quests — v1

Smallest useful loop: track player actions, roll 3 quests per player per day,
pay rewards on completion. Action ledger is foundational — future Aspirations,
Achievements, weekly events all read from the same tracking table.

## Player flow

- Log in → mail has "Today's Quests" message listing 3 challenges with rewards.
- Play normally → server hooks count progress against quests automatically.
- Quest completes → inbox message + simoleon payout, immediate.
- Midnight UTC → tomorrow's 3 quests roll, today's incomplete quests expire.

No new in-world objects. No new screens (v1). Just mail. UI on the client
is optional Phase 2 — Phase 1 ships server-only and is fully playable.

## Schema

```sql
-- 0034_daily_quests.sql
-- Append-only action log. Each player action gets a row. Used for
-- audit / leaderboards / achievement detection (recent activity).
--
-- Performance discipline:
--   * Live quest progress does NOT read this table. Event hooks update
--     fso_daily_quests.progress in-place (UPDATE ... SET progress = progress + ?)
--     so the log is write-only for the daily-quest flow.
--   * 30-day rolling retention. Nightly cron does DELETE WHERE day < N-30.
--   * Single composite index for player queries. No wide index for
--     cross-player aggregates until we actually need them.
--
-- Expected size at 100 DAU × 20 actions/day × 30d = ~60k rows (~2 MB).
CREATE TABLE `fso_action_log` (
    `id`          BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    `avatar_id`   INT UNSIGNED NOT NULL,
    `day`         INT UNSIGNED NOT NULL,        -- days-since-epoch UTC
    `action_type` TINYINT UNSIGNED NOT NULL,    -- enum below
    `value`       BIGINT UNSIGNED NOT NULL,     -- amount/count
    `parameter`   INT UNSIGNED NULL,            -- optional context (lot_id, obj guid, etc)
    `ts`          INT UNSIGNED NOT NULL,        -- unix epoch
    INDEX `idx_avatar_day_type` (`avatar_id`, `day`, `action_type`),
    INDEX `idx_purge` (`day`)
);

-- The 3 quests issued to each avatar each day, plus progress + reward state.
CREATE TABLE `fso_daily_quests` (
    `avatar_id`   INT UNSIGNED NOT NULL,
    `day`         INT UNSIGNED NOT NULL,        -- days-since-epoch UTC
    `slot`        TINYINT UNSIGNED NOT NULL,    -- 0, 1, 2
    `quest_type`  TINYINT UNSIGNED NOT NULL,    -- enum below
    `target`      BIGINT UNSIGNED NOT NULL,     -- target value to reach
    `progress`    BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `reward`      INT UNSIGNED NOT NULL,        -- simoleons paid on completion
    `parameter`   INT UNSIGNED NULL,            -- optional filter (specific job/skill type)
    `completed_ts` INT UNSIGNED NULL,           -- NULL = not done; set when completed
    `paid_ts`     INT UNSIGNED NULL,            -- NULL = not paid out yet
    PRIMARY KEY (`avatar_id`, `day`, `slot`),
    INDEX `idx_day_completed` (`day`, `completed_ts`)
);
```

## Action type enum (v1)

| ID | Name | value semantic | parameter | hook site |
|----|------|----------------|-----------|-----------|
| 1 | MONEY_EARNED | simoleons | optional: job/source GUID | every credit to avatar budget |
| 2 | SKILL_GAINED | hundredths of a point (so 50 = 0.5 levels) | skill type 0-6 | SimAntics skill primitive |
| 3 | LOT_VISITED | 1 (one row per visit) | lot_id | LotVisits.RecordVisit |
| 4 | CATALOG_BOUGHT | simoleons spent | object catalog id | catalog purchase handler |

That's enough to support v1 quest types. Easy to add more action types later
(SOCIAL, COOKED_MEAL, JOB_COMPLETED) without schema changes.

## Quest types (v1)

5 types. Roll 3 distinct ones per player per day.

| ID | Name | Template | Action source | Reward |
|----|------|----------|---------------|--------|
| 1 | EARN | Earn §X today (any source) | MONEY_EARNED sum | §X × 0.10 (capped at 5k) |
| 2 | SKILL | Gain N skill points today | SKILL_GAINED sum / 100 | §1500 |
| 3 | VISIT | Visit N unique lots today | distinct LOT_VISITED parameters | §400 × N |
| 4 | BUY | Spend §X at the catalog today | CATALOG_BOUGHT sum | §X × 0.15 (capped at 3k) |
| 5 | SOCIALIZE | Have interactions with N unique sims today | (Phase 1.5 — needs social hook) | §1500 |

**Drop #5 from Phase 1** if the social hook doesn't already plumb through to
the userApi. Check that hook before promising it.

## Target scaling

Use avatar age (days since creation) as scaling. Stops new players from being
overwhelmed and stops veterans from completing in 30 seconds.

```
EARN target  = 2000 + 500 × min(age_days, 14)          # 2k → 9k cap
SKILL target = 1 + min(age_days / 7, 2)                # 1 → 3 points
VISIT target = 2 + min(age_days / 10, 4)               # 2 → 6 lots
BUY target   = 1000 + 200 × min(age_days, 14)          # 1k → 3.8k cap
```

Tune by playtest.

## Server-side: hooks needed

Each hook just inserts into `fso_action_log` and then re-checks today's quests
for that avatar. Re-check is cheap: `SELECT * FROM fso_daily_quests WHERE
avatar_id=X AND day=today AND completed_ts IS NULL`. For each, recompute
progress from the action log and update.

| Hook | Existing site | Effort |
|------|---------------|--------|
| MONEY_EARNED | `SqlAvatars.CreditBudget` (already wraps every payout) | trivial — add one INSERT |
| SKILL_GAINED | SimAntics skill primitive, surfaces to server via gluon | needs investigation; may not pipe live skill gains today — check `BonusTask` integration |
| LOT_VISITED | `LotVisits.RecordVisit` | trivial — piggyback |
| CATALOG_BOUGHT | catalog purchase handler in `City` server | trivial |

If SKILL_GAINED hook isn't live-piped, fallback: re-derive nightly from the
`fso_avatars` skill columns delta. Slightly delayed feedback but works.

## Server-side: new cron task

`RollDailyQuestsTask` runs at 00:00 UTC.

```json
{ "cron": "0 0 * * *", "task": "roll_daily_quests", "timeout": 3600,
  "run_if_missed": true, "parameter": {} }
```

Per tick:
1. For every avatar online in last 30 days:
   - Pick 3 distinct quest types from the pool (5 in v1).
   - Compute target from avatar age.
   - INSERT 3 rows into `fso_daily_quests` for today.
2. Mail the avatar's inbox: "Today's Quests" with a description of each.
3. For yesterday's `completed_ts IS NOT NULL AND paid_ts IS NULL` quests:
   - Pay the reward into the avatar's budget.
   - Mail "Quest Reward Received" with details.
   - Set `paid_ts`.
4. Purge `fso_action_log` rows older than 90 days.

Cron timeout 3600s is generous — should run in seconds for a small shard.

## Server-side: action recheck logic (incremental, no SUM)

When a hook fires for `avatar_id` X, action type T, value V, parameter P:

```
-- 1. Append to log (audit / future use only)
INSERT INTO fso_action_log (avatar_id, day, action_type, value, parameter, ts)
VALUES (X, today, T, V, P, now());

-- 2. Increment any matching un-completed quest's progress in-place.
--    Quest match is by (avatar_id, day, quest_type maps to action_type).
--    LEAST() caps progress at target so we don't overshoot.
UPDATE fso_daily_quests
SET progress = LEAST(target, progress + <delta>),
    completed_ts = CASE WHEN progress + <delta> >= target THEN UNIX_TIMESTAMP()
                        ELSE completed_ts END
WHERE avatar_id = X
  AND day = today
  AND completed_ts IS NULL
  AND quest_type IN (<quest types this action contributes to>);
```

`<delta>` depends on quest type:
- EARN ← MONEY_EARNED: `delta = V` (simoleons earned)
- SKILL ← SKILL_GAINED: `delta = V / 100` (server-side rounding; log stores hundredths)
- VISIT ← LOT_VISITED: `delta = 1` IF parameter not in today's log already (idempotency check below)
- BUY ← CATALOG_BOUGHT: `delta = V`

### Idempotency for VISIT (unique lot count)

A single `UPDATE progress += 1` per visit would over-count repeat visits to
the same lot. Two cheap options:

**Option A (chosen)** — check the log before incrementing:
```
IF NOT EXISTS (SELECT 1 FROM fso_action_log
               WHERE avatar_id=X AND day=today AND action_type=LOT_VISITED
                 AND parameter=lot_id)
THEN insert + increment
ELSE insert only (still log, but don't bump quest progress)
```
The `idx_avatar_day_type` index makes this a fast point lookup.

**Option B** — derive `progress` for VISIT quests once at log-write time:
```sql
UPDATE fso_daily_quests q
SET q.progress = (SELECT COUNT(DISTINCT parameter)
                  FROM fso_action_log
                  WHERE avatar_id=X AND day=today AND action_type=LOT_VISITED)
WHERE q.avatar_id=X AND q.day=today AND q.quest_type=VISIT;
```
Slightly more expensive but no application-level check. With the index it's
still a sub-millisecond aggregate over ≤ N rows where N is today's visits.

Pick A in v1 (cheaper and the application code is the natural place for the
idempotency check). Switch to B only if the application logic becomes awkward.

## Client-side UI

Player-visible UI in v1. Mail covers announcements + permanent log; this
covers live progress at a glance.

### Toolbar button

Single new icon button in the existing live-mode / city-view toolbar
(alongside motives / inventory / etc).

- 32×32 PNG, TSO painterly style — scroll or checklist motif.
- Badge overlay when any quest is incomplete: small number "2/3" in the
  corner, or a glow ring if any quest is completed-but-unclaimed.
- Click opens the popup below.

Asset count: **1 new icon**. Reuses existing button chrome / hover states.

### Quest popup (`UIDailyQuestsDialog`)

Modeled after `UIAlert.cs` — modal `UIDialog`, ~400×320 px. Three quest rows
stacked vertically; standard close button top-right.

Per-row layout:

```
┌──────────────────────────────────────────────┐
│ Earn §3,000 today                            │
│ [██████████░░░░░░░░░░░] 1,800 / 3,000        │
│ Reward: §300                      [    ✓    ] │  ← stamp if completed
└──────────────────────────────────────────────┘
```

Footer: "Resets in 4h 23m" countdown + Close button.

If a quest is completed-but-unpaid (mid-day completion before nightly cron),
show a "Claim" button instead of the checkmark; clicking POSTs to
`/userapi/quests/claim/{slot}` and pays the reward immediately.

UI primitives reused: `UIDialog`, `UIProgressBar`, `UIButton`, `UILabel`,
`UICustomTooltip`. No new control classes.

### API endpoints (Phase 1, server-side, needed for the live UI)

```
GET  /userapi/quests/today
  → 200 OK
    [
      { slot: 0, type: "EARN", description: "Earn §3,000 today",
        target: 3000, progress: 1800, reward: 300, completed: false },
      { slot: 1, type: "VISIT", description: "Visit 4 unique lots today",
        target: 4, progress: 2, reward: 1600, completed: false },
      { slot: 2, type: "BUY", description: "Spend §2,000 at the catalog today",
        target: 2000, progress: 2000, reward: 300, completed: true }
    ]

POST /userapi/quests/claim/{slot}
  → 200 OK { paid: 300, new_balance: 32500 }
  → 409 if quest not yet completed
  → 410 if already claimed
```

Both gated by avatar auth cookie/JWT, same as existing `/userapi/*` servlets.
Live-progress is read on demand when the player opens the popup — no
push/websocket needed for v1.

## API endpoints (added when Phase 2 client UI lands, optional in Phase 1)

```
GET  /userapi/quests/today
  → { quests: [{ slot, type, description, target, progress, reward, completed }] }

GET  /userapi/quests/history?days=7
  → list of recent completions for stats display
```

Both gated by avatar auth (cookie / JWT, same as existing servlets).

## Implementation phases

| Phase | Scope | Status target |
|-------|-------|---------------|
| 1a | Migration 0034 + DA classes + MONEY_EARNED hook | Action ledger working end-to-end for one action |
| 1b | Add LOT_VISITED + CATALOG_BOUGHT hooks | Three actions tracked |
| 1c | `RollDailyQuestsTask` cron + 4 quest types (EARN/VISIT/BUY/SKILL-derived-nightly) | Daily roll + payout working, mail-based UX |
| 1d | API endpoints + toolbar button + `UIDailyQuestsDialog` | Live UI shipped |
| 1e | Tune targets, write quest description copy, generate toolbar icon | Playtest-ready |
| 2  | Live SKILL_GAINED hook (vs. nightly delta) | If needed for feel |
| 3  | More quest types (SOCIALIZE, COOKED_MEAL, JOB_COMPLETED) | Variety pack |

Phases 1a–1e are ~5 days of active work. v1 ships when 1e is done.

## Future plug-ins on the same infrastructure

Once `fso_action_log` exists, these become small additions:

- **Achievements** (lifetime totals from action log)
- **Aspirations** (longer ladder, action log sums)
- **Weekly tournaments** (top 10 by action type X this week)
- **Leaderboards** (`SELECT avatar_id, SUM(value) FROM fso_action_log
  WHERE action_type=1 AND day>=this_week GROUP BY avatar_id`)
- **Personalized event recommendations** ("you cook a lot, here's a new oven")

All without further schema changes. The action ledger is the right primitive
to invest in first.

## Tracking what's done

When implementation starts, each phase's PR/commit should reference the
phase letter in the design doc so we can map back to scope.
