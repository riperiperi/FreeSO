CREATE TABLE `fso_generic_avatar_participation` (
  `participation_name` TEXT(64) NOT NULL,
  `participation_avatar` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`participation_name`(64), `participation_avatar`),
  INDEX `fso_generic_ava_idx` (`participation_avatar` ASC),
  INDEX `fso_generic_name_idx` (`participation_name`(64) ASC),
  CONSTRAINT `fso_generic_avatarp_id`
    FOREIGN KEY (`participation_avatar`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'If you need a generic way to track avatar participation in things, here it is. Useful for events and tasks that don\'t have their own per-avatar tracking and need it.';

ALTER TABLE `fso_tasks` 
CHANGE COLUMN `task_type` `task_type` ENUM('prune_database', 'bonus', 'shutdown', 'job_balance', 'multi_check', 'prune_abandoned_lots', 'neighborhood_tick', 'birthday_gift') NOT NULL ;