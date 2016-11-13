-- User table, does not include password info so we dont return this into RAM unless we absolutely need to. Also will help do SSO eventually
CREATE TABLE IF NOT EXISTS `fso_users` (
  `user_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `email` varchar(120) NOT NULL,
  `user_state` enum('valid','email_confirm','moderated') NOT NULL DEFAULT 'email_confirm',
  `register_date` int(10) NOT NULL,
  `is_admin` tinyint(3) NOT NULL,
  `is_moderator` tinyint(3) NOT NULL,
  `is_banned` tinyint(3) NOT NULL,
  `register_ip` varchar(50) NOT NULL DEFAULT '127.0.0.1',
  `last_ip` varchar(50) NOT NULL DEFAULT '127.0.0.1',
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

-- Stores password hashes
CREATE TABLE IF NOT EXISTS `fso_user_authenticate` (
  `user_id` int(10) unsigned NOT NULL,
  `scheme_class` varchar(75) NOT NULL,
  `data` mediumblob NOT NULL,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `fk_users_pass` FOREIGN KEY (`user_id`) REFERENCES `fso_users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Default user
INSERT INTO fso_users VALUES (1, 'admin', 'admin@freeso.org', 'valid', 1439646790, 1, 1, 0, '127.0.0.1', '127.0.0.1') ON DUPLICATE KEY UPDATE user_id = user_id;

-- Default user password
INSERT INTO fso_user_authenticate VALUES (1, 'Rfc2898', 0x10E28CDC7DC433309503EC95A7222BF4CDE2C5DD386FB91CE632EBF9DC22B6EC398AA2FE6F2B0E60278493283CD6886A2C7072F97ACE3E06EF0EE1E93388A4A793A44DF0C124AF01364F55B28CCE1927B9) ON DUPLICATE KEY UPDATE user_id = user_id;

