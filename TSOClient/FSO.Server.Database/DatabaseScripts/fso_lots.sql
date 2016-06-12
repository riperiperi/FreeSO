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

CREATE TABLE `fso_lot_claims` (
	`claim_id` INT(11) NOT NULL AUTO_INCREMENT,
	`shard_id` INT(11) NOT NULL,
	`lot_id` INT(10) UNSIGNED NOT NULL,
	`owner` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`claim_id`),
	UNIQUE INDEX `shard_id_lot_id` (`shard_id`, `lot_id`)
)
COMMENT='Represents a lot servers claim on a lot.'
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE `fso_lot_server_tickets` (
	`ticket_id` VARCHAR(36) NOT NULL,
	`user_id` INT(10) UNSIGNED NOT NULL,
	`date` INT(10) UNSIGNED NOT NULL,
	`ip` VARCHAR(50) NOT NULL,
	`avatar_id` INT(10) UNSIGNED NOT NULL,
	`lot_id` INT(10) NOT NULL,
	`avatar_claim_id` INT(11) NOT NULL,
	`avatar_claim_owner` VARCHAR(50) NOT NULL,
	PRIMARY KEY (`ticket_id`),
	INDEX `FK_fso_lot_server_tickets_fso_lots` (`lot_id`),
	CONSTRAINT `FK_fso_lot_server_tickets_fso_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`),
	CONSTRAINT `FK_fso_lot_server_tickets_fso_avatar_claims` FOREIGN KEY (`avatar_claim_id`) REFERENCES `fso_avatar_claims` (`avatar_claim_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;