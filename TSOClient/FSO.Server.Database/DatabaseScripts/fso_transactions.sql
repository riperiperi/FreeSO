CREATE TABLE IF NOT EXISTS `fso_transactions` (
  `from_id` int(11) unsigned NOT NULL,
  `to_id` int(11) unsigned NOT NULL,
  `transaction_type` int(11) unsigned NOT NULL,
  `value` int(11) unsigned NOT NULL,
  `time_first` int(11) unsigned NOT NULL,
  `time_last` int(11) unsigned NOT NULL,
  `count` int(11) unsigned NOT NULL,
  `value_per_hour` double unsigned NOT NULL,
  PRIMARY KEY (`from_id`,`to_id`,`transaction_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;