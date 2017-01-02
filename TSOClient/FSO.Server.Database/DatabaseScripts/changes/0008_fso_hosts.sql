CREATE TABLE IF NOT EXISTS `fso_hosts` (
  `call_sign` varchar(50) NOT NULL,
  `role` enum('city','lot','task') NOT NULL,
  `status` enum('up','down') NOT NULL,
  `internal_host` varchar(100) NOT NULL,
  `public_host` varchar(100) NOT NULL,
  `time_boot` datetime NOT NULL,
  `shard_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`call_sign`),
  KEY `FK_fso_hosts_fso_shards` (`shard_id`),
  CONSTRAINT `FK_fso_hosts_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`) ON DELETE CASCADE ON UPDATE CASCADE
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;