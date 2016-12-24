CREATE TABLE IF NOT EXISTS `fso_lot_visits` (
  `lot_visit_id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `avatar_id` int(11) unsigned NOT NULL,
  `lot_id` int(11) NOT NULL,
  `type` enum('owner','roommate','visitor') DEFAULT NULL,
  `status` enum('active','closed','failed') NOT NULL,
  `time_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `time_closed` datetime DEFAULT NULL,
  PRIMARY KEY (`lot_visit_id`),
  KEY `FK_fso_avatar_audits_fso_avatars` (`avatar_id`),
  KEY `FK_fso_lot_visitors_fso_lots` (`lot_id`),
  CONSTRAINT `FK_fso_avatar_audits_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_fso_lot_visitors_fso_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE CASCADE ON UPDATE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE FUNCTION `fso_lot_visits_create`(`p_avatar_id` INT, `p_lot_id` INT, `p_visitor_type` VARCHAR(50)) RETURNS int(11)
    READS SQL DATA
BEGIN
	#Error any open active visit, can only have one active
	UPDATE fso_lot_visits SET `status` = 'failed', time_closed = current_timestamp WHERE avatar_id = p_avatar_id AND `status` = 'active';
	#Record visit
	INSERT INTO fso_lot_visits (avatar_id, lot_id, type, status) VALUES (p_avatar_id, p_lot_id, p_visitor_type, 'active'); 
	RETURN LAST_INSERT_ID();
END;

CREATE TABLE IF NOT EXISTS `fso_lot_top_100` (
  `category` enum('none','welcome','money','skills','services','entertainment','romance','shopping','games','offbeat','residence') NOT NULL,
  `rank` tinyint(4) unsigned NOT NULL,
  `shard_id` int(11) NOT NULL,
  `lot_id` int(11) DEFAULT NULL,
  `minutes` int(11) DEFAULT NULL,
  `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`category`,`rank`,`shard_id`),
  KEY `FK_fso_lots_top_100_fso_shards` (`shard_id`),
  KEY `FK_fso_lots_top_100_fso_lots` (`lot_id`),
  CONSTRAINT `FK_fso_lots_top_100_fso_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_fso_lots_top_100_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;