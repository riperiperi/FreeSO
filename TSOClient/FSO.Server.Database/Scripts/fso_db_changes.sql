DROP TABLE IF EXISTS `fso_db_changes`;

CREATE TABLE `fso_db_changes` (
	`filename` VARCHAR(100) NOT NULL,
	`date` INT(10) NOT NULL,
	`hash` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`filename`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;
