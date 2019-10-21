CREATE TABLE IF NOT EXISTS `fso_global_cooldowns` (
  `object_guid` int(11) unsigned NOT NULL,
  `avatar_id` int(10) unsigned NOT NULL,
  `user_id` int(10) unsigned NOT NULL,
  `category` int(10) unsigned NOT NULL,
  `expiry` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`object_guid`, `avatar_id`, `user_id`, `category`),
  CONSTRAINT `FK_global_cooldowns_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_global_cooldowns_fso_users` FOREIGN KEY (`user_id`) REFERENCES `fso_users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
)