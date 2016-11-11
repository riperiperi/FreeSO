CREATE TABLE IF NOT EXISTS `fso_bookmarks` (
  `avatar_id` int(10) unsigned NOT NULL,
  `type` tinyint(3) unsigned NOT NULL,
  `target_id` int(10) unsigned NOT NULL,
  PRIMARY KEY (`avatar_id`,`type`,`target_id`),
  KEY `FK_fso_bookmarks_fso_avatars_target` (`target_id`),
  CONSTRAINT `FK_fso_bookmarks_fso_avatars_src` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`),
  CONSTRAINT `FK_fso_bookmarks_fso_avatars_target` FOREIGN KEY (`target_id`) REFERENCES `fso_avatars` (`avatar_id`)
) 
COLLATE='utf8_general_ci'
ENGINE=InnoDB;