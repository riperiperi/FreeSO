CREATE TABLE `fso_email_confirm` (
	`type` ENUM('email','password') NULL DEFAULT NULL,
	`email` VARCHAR(50) NULL DEFAULT NULL,
	`token` VARCHAR(50) NULL DEFAULT NULL,
	`expires` INT(11) NULL DEFAULT NULL,
	`verified` TINYINT(4) NULL DEFAULT NULL
)
COMMENT='Table to control necessary email confirmation for registration or password reset.';