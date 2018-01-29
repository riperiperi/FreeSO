CREATE TABLE `fso_email_confirm` (
	`type` ENUM('email','password') NOT NULL DEFAULT 'email',
	`email` VARCHAR(50) NULL DEFAULT NULL,
	`token` VARCHAR(50) NULL DEFAULT NULL,
	`expires` INT(11) NULL DEFAULT NULL
)
COMMENT='Table to control necessary email confirmation for registration or password reset.'
COLLATE='latin1_swedish_ci'
ENGINE=InnoDB
;