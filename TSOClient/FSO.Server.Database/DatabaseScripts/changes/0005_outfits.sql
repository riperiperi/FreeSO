CREATE TABLE IF NOT EXISTS `fso_outfits` (
  `outfit_id` int(11) NOT NULL AUTO_INCREMENT,
  `avatar_owner` int(10) unsigned DEFAULT NULL,
  `object_owner` int(11) unsigned DEFAULT NULL,
  `asset_id` bigint(20) unsigned DEFAULT NULL,
  `sale_price` int(11) NOT NULL,
  `purchase_price` int(11) NOT NULL,
  PRIMARY KEY (`outfit_id`),
  KEY `FK_fso_outfits_fso_avatars` (`avatar_owner`),
  KEY `FK_fso_outfits_fso_objects` (`object_owner`),
  CONSTRAINT `FK_fso_outfits_fso_avatars` FOREIGN KEY (`avatar_owner`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE,
  CONSTRAINT `FK_fso_outfits_fso_objects` FOREIGN KEY (`object_owner`) REFERENCES `fso_objects` (`object_id`) ON DELETE CASCADE
) 
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TRIGGER `fso_outfits_before_insert` BEFORE INSERT ON `fso_outfits` FOR EACH ROW BEGIN
 IF NEW.object_owner IS NOT NULL THEN
	 IF (SELECT COUNT(*) FROM fso_outfits o WHERE NEW.object_owner = o.object_owner) >= 20 THEN
	  SIGNAL SQLSTATE '45000'
	  SET MESSAGE_TEXT = 'Cannot have more than 20 outfits in a rack.';
	 END IF;
 END IF;
END;
