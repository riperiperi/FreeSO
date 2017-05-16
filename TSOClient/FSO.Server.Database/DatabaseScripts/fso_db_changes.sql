CREATE TABLE IF NOT EXISTS `fso_db_changes` (
	`id` VARCHAR(100) NOT NULL,
	`filename` VARCHAR(100) NOT NULL,
	`date` INT UNSIGNED NOT NULL,
	`hash` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`id`),
	UNIQUE INDEX `filename` (`filename`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;