CREATE TABLE `fso_lots` (
	`lot_id` INT(11) NOT NULL AUTO_INCREMENT,
	`shard_id` INT(11) NOT NULL,
	`name` VARCHAR(50) NOT NULL COLLATE 'utf8_general_ci',
	`description` VARCHAR(500) NOT NULL COLLATE 'utf8_general_ci',
	`owner_id` INT(10) UNSIGNED NOT NULL,
	`location` INT(10) UNSIGNED NOT NULL,
	`neighborhood_id` INT(11) NULL DEFAULT NULL,
	`created_date` INT(10) UNSIGNED NOT NULL,
	`category_change_date` INT(10) UNSIGNED NOT NULL,
	`category` ENUM('none','welcome','money','skills','services','entertainment','romance','shopping','games','offbeat','residence') NOT NULL,
	`buildable_area` INT(10) UNSIGNED NOT NULL,
	PRIMARY KEY (`lot_id`),
	UNIQUE INDEX `shard_id_name` (`shard_id`, `name`),
	UNIQUE INDEX `shard_id_location` (`shard_id`, `location`),
	CONSTRAINT `FK_fso_lots_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE `fso_lots_roomates` (
	`lot_id` INT(11) NOT NULL,
	`avatar_id` INT(11) UNSIGNED NOT NULL,
	`created` INT(10) UNSIGNED NULL DEFAULT NULL,
	PRIMARY KEY (`lot_id`, `avatar_id`),
	UNIQUE INDEX `avatar_id` (`avatar_id`),
	CONSTRAINT `FK_fso_lots_roomates_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`),
	CONSTRAINT `FK_fso_lots_roomates_fso_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`)
)
ENGINE=InnoDB;
