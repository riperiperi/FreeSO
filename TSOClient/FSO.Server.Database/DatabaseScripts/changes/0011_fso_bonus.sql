CREATE TABLE IF NOT EXISTS `fso_bonus` (
  `avatar_id` int(10) unsigned NOT NULL DEFAULT '0',
  `time_issued` date NOT NULL,
  `bonus_visitor` int(11) NOT NULL,
  `bonus_property` int(11) NOT NULL,
  `bonus_sim` int(11) NOT NULL,
  PRIMARY KEY (`avatar_id`,`time_issued`),
  CONSTRAINT `FK_fso_bonus_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;

CREATE TRIGGER `fso_bonus_after_insert` AFTER INSERT ON `fso_bonus` FOR EACH ROW BEGIN
	UPDATE fso_avatars a SET budget = (budget + NEW.bonus_visitor + NEW.bonus_property + NEW.bonus_sim) WHERE a.avatar_id = NEW.avatar_id;
END;