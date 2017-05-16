CREATE TABLE IF NOT EXISTS `fso_avatars` (
  `avatar_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `shard_id` int(11) NOT NULL,
  `user_id` int(10) unsigned NOT NULL,
  `name` varchar(24) NOT NULL,
  `gender` enum('male','female') NOT NULL,
  `date` int(10) unsigned NOT NULL,
  `skin_tone` tinyint(3) unsigned NOT NULL,
  `head` bigint(20) unsigned NOT NULL,
  `body` bigint(20) unsigned NOT NULL,
  `body_swimwear` bigint(20) unsigned NOT NULL DEFAULT '2985002270733',
  `body_sleepwear` bigint(20) unsigned NOT NULL DEFAULT '2980707303437',
  `body_current` bigint(20) unsigned NOT NULL DEFAULT '0',
  `description` varchar(500) NOT NULL,
  `budget` int(11) NOT NULL DEFAULT '0',
  `privacy_mode` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `motive_data` binary(32) NOT NULL DEFAULT '0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0',
  `skilllock` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `lock_mechanical` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lock_cooking` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lock_charisma` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lock_logic` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lock_body` smallint(5) unsigned NOT NULL DEFAULT '0',
  `lock_creativity` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_mechanical` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_cooking` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_charisma` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_logic` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_body` smallint(5) unsigned NOT NULL DEFAULT '0',
  `skill_creativity` smallint(5) unsigned NOT NULL DEFAULT '0',
  `current_job` smallint(5) unsigned NOT NULL DEFAULT '0',
  `is_ghost` smallint(5) unsigned NOT NULL DEFAULT '0',
  `ticker_death` smallint(5) unsigned NOT NULL DEFAULT '0',
  `ticker_gardener` smallint(5) unsigned NOT NULL DEFAULT '0',
  `ticker_maid` smallint(5) unsigned NOT NULL DEFAULT '0',
  `ticker_repairman` smallint(5) unsigned NOT NULL DEFAULT '0',
  `moderation_level` tinyint(3) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`avatar_id`),
  UNIQUE KEY `shard_id_name` (`shard_id`,`name`),
  KEY `FK_shard` (`shard_id`),
  KEY `FK_avatar_users_idx` (`user_id`),
  CONSTRAINT `FK_avatar_users` FOREIGN KEY (`user_id`) REFERENCES `fso_users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_shard` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE `fso_avatar_claims` (
	`avatar_claim_id` INT(11) NOT NULL AUTO_INCREMENT,
	`avatar_id` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`owner` VARCHAR(50) NOT NULL DEFAULT '0',
	`location` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	PRIMARY KEY (`avatar_claim_id`),
	UNIQUE INDEX `avatar_id` (`avatar_id`),
	CONSTRAINT `FK_fso_avatar_claims_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`)
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `fso_joblevels` (
  `avatar_id` int(10) unsigned NOT NULL,
  `job_type` smallint(5) unsigned NOT NULL,
  `job_experience` smallint(5) unsigned NOT NULL,
  `job_level` smallint(5) unsigned NOT NULL,
  `job_sickdays` smallint(5) unsigned NOT NULL,
  `job_statusflags` smallint(5) unsigned NOT NULL,
  PRIMARY KEY (`avatar_id`,`job_type`),
  CONSTRAINT `FK_job_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `fso_relationships` (
  `from_id` int(10) unsigned NOT NULL,
  `to_id` int(10) unsigned NOT NULL,
  `value` int(10) NOT NULL,
  `index` int(10) unsigned NOT NULL,
  `comment_id` int(11) unsigned DEFAULT NULL,
  `date` int(11) unsigned NOT NULL,
  PRIMARY KEY (`from_id`,`to_id`,`index`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;