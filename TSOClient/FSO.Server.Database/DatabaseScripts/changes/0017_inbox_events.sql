CREATE TABLE `fso_inbox` (
  `message_id` INT NOT NULL AUTO_INCREMENT,
  `sender_id` INT UNSIGNED NOT NULL,
  `target_id` INT UNSIGNED NOT NULL,
  `subject` VARCHAR(128) NOT NULL,
  `body` VARCHAR(1500) NOT NULL,
  `sender_name` VARCHAR(64) NOT NULL,
  `time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `msg_type` INT NOT NULL DEFAULT 0,
  `msg_subtype` INT NOT NULL DEFAULT 0,
  `read_state` INT NOT NULL DEFAULT 0,
  `reply_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`message_id`),
  INDEX `index_target_inbox` (`target_id` ASC),
  CONSTRAINT `fso_target_avatar`
    FOREIGN KEY (`target_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE);

CREATE TABLE `fso_events` (
  `event_id` INT NOT NULL AUTO_INCREMENT,
  `title` VARCHAR(128) NULL,
  `description` VARCHAR(500) NULL,
  `start_day` DATETIME NOT NULL,
  `end_day` DATETIME NOT NULL,
  `type` ENUM('mail_only', 'free_object', 'free_money', 'free_green', 'obj_tuning') NOT NULL,
  `value` INT NOT NULL,
  `value2` INT NOT NULL DEFAULT 0,
  `mail_subject` VARCHAR(128) NULL,
  `mail_message` VARCHAR(1000) NULL,
  `mail_sender` INT NULL,
  `mail_sender_name` VARCHAR(64) NULL,
  PRIMARY KEY (`event_id`));

CREATE TABLE `fso_event_participation` (
  `event_id` INT NOT NULL,
  `user_id` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`event_id`, `user_id`),
  INDEX `fso_event_part_user_idx` (`user_id` ASC),
  CONSTRAINT `fso_event_part_id`
    FOREIGN KEY (`event_id`)
    REFERENCES `fso_events` (`event_id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `fso_event_part_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE);