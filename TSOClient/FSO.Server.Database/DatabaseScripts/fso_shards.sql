CREATE TABLE IF NOT EXISTS `fso_shards` (
	`shard_id` INT NOT NULL,
	`name` VARCHAR(100) NOT NULL,
	`rank` INT NOT NULL,
	`map` VARCHAR(10) NOT NULL,
	`status` ENUM('up','down','busy','full','closed','frontier') NOT NULL,
	`internal_host` VARCHAR(100) NOT NULL,
	`public_host` VARCHAR(100) NOT NULL,
	PRIMARY KEY (`shard_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

-- Default shard
INSERT INTO fso_shards VALUES (1, 'Alphavilla', 1, '0001', 'up', '127.0.0.1:7777', '127.0.0.1:7777') ON DUPLICATE KEY UPDATE shard_id = shard_id;

CREATE TABLE IF NOT EXISTS `fso_shard_tickets` (
	`ticket_id` VARCHAR(36) NOT NULL,
	`user_id` INT UNSIGNED NOT NULL,
	`date` INT UNSIGNED NOT NULL,
	`ip` VARCHAR(50) NOT NULL,
	`avatar_id` INT UNSIGNED NOT NULL,
	PRIMARY KEY (`ticket_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;