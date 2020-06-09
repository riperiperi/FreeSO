CREATE TABLE `fso_election_freevotes` (
  `avatar_id` INT UNSIGNED NOT NULL,
  `neighborhood_id` INT NOT NULL,
  `cycle_id` INT UNSIGNED NOT NULL,
  `date` INT NOT NULL,
  `expire_date` INT NOT NULL,
  PRIMARY KEY (`avatar_id`),
  INDEX `fso_freevote_cycle_idx` (`cycle_id` ASC),
  INDEX `fso_freevote_nhood_idx` (`neighborhood_id` ASC),
  CONSTRAINT `fso_freevote_avatar`
    FOREIGN KEY (`avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_freevote_cycle`
    FOREIGN KEY (`cycle_id`)
    REFERENCES `fso_election_cycles` (`cycle_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_freevote_nhood`
    FOREIGN KEY (`neighborhood_id`)
    REFERENCES `fso_neighborhoods` (`neighborhood_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'When a neighborhood is ineligible for an election, its residents get to choose a neighborhood election to participate in. Entries in this table allow avatars to vote in an election when they do not live in its neighborhood. Entries in this table should expire when the linked cycle ends.';

ALTER TABLE `fso_election_freevotes` 
CHANGE COLUMN `date` `date` INT(11) UNSIGNED NOT NULL ,
CHANGE COLUMN `expire_date` `expire_date` INT(11) UNSIGNED NOT NULL ;

ALTER TABLE `fso_election_votes` 
ADD COLUMN `value` INT NOT NULL DEFAULT 1 COMMENT 'The value of this vote. Some votes can be worth more than others (eg. free votes are worth less than normal ones)' AFTER `date`;

CREATE TABLE `fso_auth_attempts` (
  `attempt_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `ip` VARCHAR(100) NOT NULL,
  `user_id` INT UNSIGNED NOT NULL,
  `expire_time` INT UNSIGNED NOT NULL,
  `count` INT NOT NULL DEFAULT 0,
  `active` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  `invalidated` TINYINT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`attempt_id`),
  INDEX `fk_user_attempt_idx` (`user_id` ASC),
  CONSTRAINT `fk_user_attempt`
    FOREIGN KEY (`user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE);