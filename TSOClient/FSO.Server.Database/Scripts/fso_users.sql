DROP TABLE IF EXISTS `fso_users`;

CREATE TABLE `fso_users` (
	`user_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`username` VARCHAR(50) NOT NULL,
	`email` VARCHAR(120) NOT NULL,
	`user_state` ENUM('valid','email_confirm','moderated') NOT NULL DEFAULT 'email_confirm',
	`register_date` INT(10) NOT NULL,
	`is_admin` TINYINT(3) NOT NULL,
	`is_moderator` TINYINT(3) NOT NULL,
	`is_banned` TINYINT(3) NOT NULL,
	PRIMARY KEY (`user_id`),
	UNIQUE INDEX `username` (`username`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;