CREATE TABLE IF NOT EXISTS `fso_objects` (
  `object_id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `shard_id` int(11) NOT NULL,
  `owner_id` int(11) unsigned DEFAULT NULL,
  `lot_id` int(11) DEFAULT NULL,
  `dyn_obj_name` varchar(64) NOT NULL,
  `type` int(11) unsigned NOT NULL,
  `graphic` smallint(5) unsigned NOT NULL,
  `value` int(11) unsigned NOT NULL,
  `wear` smallint(5) unsigned NOT NULL DEFAULT '0',
  `budget` int(11) NOT NULL DEFAULT '0',
  `dyn_flags_1` bigint(20) unsigned NOT NULL DEFAULT '0',
  `dyn_flags_2` bigint(20) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`object_id`),
  KEY `FK_owner_idx` (`owner_id`),
  KEY `FK_obj_shard_idx` (`shard_id`),
  KEY `FK_obj_lot_idx` (`lot_id`),
  CONSTRAINT `FK_obj_lot` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_obj_shard` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_owner` FOREIGN KEY (`owner_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE SET NULL ON UPDATE CASCADE
)
AUTO_INCREMENT=16777216
COLLATE='utf8_general_ci'
ENGINE=InnoDB;