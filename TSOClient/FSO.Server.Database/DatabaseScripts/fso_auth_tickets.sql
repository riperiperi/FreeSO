CREATE TABLE IF NOT EXISTS `fso_auth_tickets` (
	`ticket_id` VARCHAR(36) NOT NULL,
	`user_id` INT(10) UNSIGNED NOT NULL,
	`date` INT(10) UNSIGNED NOT NULL,
	`ip` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`ticket_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;