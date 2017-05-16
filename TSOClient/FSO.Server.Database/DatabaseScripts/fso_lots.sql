CREATE TABLE IF NOT EXISTS `fso_lots` (
  `lot_id` int(11) NOT NULL AUTO_INCREMENT,
  `shard_id` int(11) NOT NULL,
  `name` varchar(50) NOT NULL,
  `description` varchar(500) NOT NULL,
  `owner_id` int(10) unsigned NOT NULL,
  `location` int(10) unsigned zerofill NOT NULL,
  `neighborhood_id` int(11) DEFAULT NULL,
  `created_date` int(10) unsigned NOT NULL,
  `category_change_date` int(11) unsigned NOT NULL,
  `category` enum('none','welcome','money','skills','services','entertainment','romance','shopping','games','offbeat','residence') NOT NULL,
  `buildable_area` int(10) unsigned NOT NULL,
  `ring_backup_num` tinyint(4) NOT NULL DEFAULT '-1',
  `admit_mode` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `move_flags` tinyint(3) DEFAULT '1',
  PRIMARY KEY (`lot_id`),
  UNIQUE KEY `shard_id_name` (`shard_id`,`name`),
  UNIQUE KEY `shard_id_location` (`shard_id`,`location`),
  UNIQUE KEY `location_UNIQUE` (`location`),
  CONSTRAINT `FK_fso_lots_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
) 
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `fso_roommates` (
  `avatar_id` int(10) unsigned NOT NULL,
  `lot_id` int(11) NOT NULL,
  `permissions_level` tinyint(3) unsigned NOT NULL,
  `is_pending` tinyint(3) unsigned NOT NULL,
  PRIMARY KEY (`avatar_id`,`lot_id`),
  KEY `FK_roommate_lots_idx` (`lot_id`),
  CONSTRAINT `FK_roommate_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_roommate_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `fso_lot_claims` (
  `claim_id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `shard_id` int(11) NOT NULL,
  `lot_id` int(10) NOT NULL,
  `owner` varchar(50) NOT NULL,
  PRIMARY KEY (`claim_id`),
  UNIQUE KEY `shard_id_lot_id` (`shard_id`,`lot_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Represents a lot servers claim on a lot.';

CREATE TABLE IF NOT EXISTS `fso_lot_server_tickets` (
  `ticket_id` varchar(36) NOT NULL,
  `user_id` int(10) unsigned NOT NULL,
  `date` int(10) unsigned NOT NULL,
  `ip` varchar(50) NOT NULL,
  `avatar_id` int(10) unsigned NOT NULL,
  `lot_id` int(10) NOT NULL COMMENT 'Note: need not necessarily be a real lot! Tickets can be granted for runtime lots, such as job lots. These are marked with the bit flag 0x40000000.',
  `avatar_claim_id` int(11) NOT NULL,
  `avatar_claim_owner` varchar(50) NOT NULL,
  `lot_owner` varchar(50) NOT NULL,
  PRIMARY KEY (`ticket_id`),
  KEY `FK_fso_lot_server_tickets_fso_avatar_claims` (`avatar_claim_id`),
  CONSTRAINT `FK_fso_lot_server_tickets_fso_avatar_claims` FOREIGN KEY (`avatar_claim_id`) REFERENCES `fso_avatar_claims` (`avatar_claim_id`) ON DELETE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `fso_lot_admit` (
  `lot_id` int(11) NOT NULL,
  `avatar_id` int(10) unsigned NOT NULL,
  `admit_type` tinyint(3) unsigned NOT NULL,
  PRIMARY KEY (`avatar_id`,`lot_id`),
  KEY `FK_lot_idx` (`lot_id`),
  KEY `FK_avatar_idx` (`avatar_id`),
  CONSTRAINT `FK_admit_avatar` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_admit_lot` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE CASCADE ON UPDATE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;
