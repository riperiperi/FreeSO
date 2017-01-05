CREATE TABLE IF NOT EXISTS `fso_ip_ban` (
  `user_id` INT UNSIGNED NOT NULL,
  `ip_address` VARCHAR(100) NULL,
  `banreason` VARCHAR(500) NULL,
  `end_date` INT(10) NOT NULL DEFAULT 0,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `fso_ban_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE);