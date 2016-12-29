CREATE TABLE IF NOT EXISTS `fso_tasks` (
  `task_id` int(11) NOT NULL AUTO_INCREMENT,
  `task_type` enum('prune_database','bonus') NOT NULL,
  `task_status` enum('in_progress','completed','failed') NOT NULL,
  `time_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `time_completed` datetime DEFAULT NULL,
  `shard_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`task_id`),
  KEY `FK_fso_tasks_fso_shards` (`shard_id`),
  CONSTRAINT `FK_fso_tasks_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`) ON DELETE CASCADE ON UPDATE CASCADE
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;