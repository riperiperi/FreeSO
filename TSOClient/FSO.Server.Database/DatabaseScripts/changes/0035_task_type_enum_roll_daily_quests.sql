-- Extends the fso_tasks.task_type ENUM to accept 'roll_daily_quests',
-- the cron task added in DbTaskType (C# enum) for the daily-quests
-- system. Without this, TaskEngine.Run's INSERT INTO fso_tasks
-- (task_type, …) is rejected by MySQL with "Data truncated for column
-- task_type" before the task body ever runs — surfaces as a vague
-- "unknown error starting task roll_daily_quests" in the server log.
--
-- Mirrors the previous task-enum extensions (0023_neighborhoods,
-- 0029_generic_participation). Existing rows are unaffected — ENUM
-- additions are a metadata-only schema change.
ALTER TABLE `fso_tasks`
  CHANGE COLUMN `task_type` `task_type`
  ENUM(
    'prune_database',
    'bonus',
    'shutdown',
    'job_balance',
    'multi_check',
    'prune_abandoned_lots',
    'neighborhood_tick',
    'birthday_gift',
    'roll_daily_quests'
  ) NOT NULL;