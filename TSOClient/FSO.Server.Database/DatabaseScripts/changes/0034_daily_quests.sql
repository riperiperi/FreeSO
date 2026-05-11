-- Daily quests + action log. See edenso_server_data/design_daily_quests_v1.md
-- for full spec. Two tables:
--
--   fso_action_log    audit/history of player actions (write-only for quest
--                     flow; live progress is in fso_daily_quests). 30-day
--                     rolling retention purged by RollDailyQuestsTask.
--
--   fso_daily_quests  three rolled quests per avatar per day with running
--                     progress, target, reward, completed/paid timestamps.

CREATE TABLE `fso_action_log` (
    `id`          BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `avatar_id`   INT UNSIGNED NOT NULL,
    `day`         INT UNSIGNED NOT NULL COMMENT 'days-since-epoch UTC',
    `action_type` TINYINT UNSIGNED NOT NULL COMMENT '1=MONEY_EARNED 2=SKILL_GAINED 3=LOT_VISITED 4=CATALOG_BOUGHT',
    `value`       BIGINT UNSIGNED NOT NULL,
    `parameter`   INT UNSIGNED DEFAULT NULL COMMENT 'lot_id, object guid, skill type — context for the action',
    `ts`          INT UNSIGNED NOT NULL COMMENT 'unix epoch',
    PRIMARY KEY (`id`),
    INDEX `idx_avatar_day_type` (`avatar_id`, `day`, `action_type`),
    INDEX `idx_purge` (`day`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `fso_daily_quests` (
    `avatar_id`      INT UNSIGNED NOT NULL,
    `day`            INT UNSIGNED NOT NULL COMMENT 'days-since-epoch UTC',
    `slot`           TINYINT UNSIGNED NOT NULL COMMENT '0,1,2',
    `quest_type`     TINYINT UNSIGNED NOT NULL COMMENT '1=EARN 2=SKILL 3=VISIT 4=BUY',
    `target`         BIGINT UNSIGNED NOT NULL COMMENT 'progress threshold for completion',
    `progress`       BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `reward`         INT UNSIGNED NOT NULL COMMENT 'simoleons paid on completion (capped per design doc)',
    `parameter`      INT UNSIGNED DEFAULT NULL COMMENT 'optional filter — reserved for typed quests (specific skill, specific job, etc.)',
    `completed_ts`   INT UNSIGNED DEFAULT NULL COMMENT 'unix epoch, NULL until completed',
    `paid_ts`        INT UNSIGNED DEFAULT NULL COMMENT 'unix epoch, NULL until reward paid',
    PRIMARY KEY (`avatar_id`, `day`, `slot`),
    INDEX `idx_day_completed` (`day`, `completed_ts`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;