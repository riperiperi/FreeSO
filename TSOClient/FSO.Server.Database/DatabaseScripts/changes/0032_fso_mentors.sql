CREATE TABLE IF NOT EXISTS `fso_mentors` (
  `user_id` int(10) unsigned NOT NULL,
  `mentor_status` tinyint(3) unsigned NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`)
)