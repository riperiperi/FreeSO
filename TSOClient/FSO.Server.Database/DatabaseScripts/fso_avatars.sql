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
