CREATE TABLE `fso_avatars` (
	`avatar_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`shard_id` INT(11) NOT NULL,
	`user_id` INT(10) UNSIGNED NOT NULL,
	`name` VARCHAR(24) NOT NULL,
	`gender` ENUM('male','female') NOT NULL,
	`date` INT(10) UNSIGNED NOT NULL,
	`skin_tone` TINYINT(3) UNSIGNED NOT NULL,
	`head` BIGINT(20) UNSIGNED NOT NULL,
	`body` BIGINT(20) UNSIGNED NOT NULL,
	`description` VARCHAR(500) NOT NULL,
	PRIMARY KEY (`avatar_id`),
	UNIQUE INDEX `shard_id_name` (`shard_id`, `name`),
	INDEX `FK_shard` (`shard_id`),
	CONSTRAINT `FK_shard` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE `fso_avatar_claims` (
	`avatar_claim_id` INT(11) NOT NULL AUTO_INCREMENT,
	`avatar_id` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`owner` VARCHAR(50) NOT NULL DEFAULT '0',
	`location` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY (`avatar_claim_id`),
	UNIQUE INDEX `avatar_id` (`avatar_id`),
	CONSTRAINT `FK_fso_avatar_claims_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;