ALTER TABLE `fso_transactions` 
DROP COLUMN `time_last`,
CHANGE COLUMN `time_first` `day` INT(11) UNSIGNED NOT NULL AFTER `transaction_type`,
CHANGE COLUMN `value_per_hour` `value_per_hour` DOUBLE UNSIGNED NULL ,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`from_id`, `to_id`, `transaction_type`, `day`),
ADD INDEX `fso_transaction_from` (`from_id` ASC),
ADD INDEX `fso_transaction_to` (`to_id` ASC),
ADD INDEX `fso_transaction_type` (`transaction_type` ASC);

CREATE TABLE `fso_tuning` (
  `tuning_type` VARCHAR(128) NOT NULL,
  `tuning_table` INT NOT NULL,
  `tuning_index` INT NOT NULL,
  `value` FLOAT NOT NULL,
  `owner_type` ENUM('STATIC', 'DYNAMIC', 'EVENT') NOT NULL DEFAULT 'STATIC',
  `owner_id` INT NULL DEFAULT NULL,
  INDEX `tuning_by_owner` (`owner_type` ASC, `owner_id` ASC));

CREATE TABLE `fso_dyn_payouts` (
  `day` INT NOT NULL,
  `skilltype` INT NOT NULL,
  `multiplier` FLOAT NOT NULL DEFAULT 1,
  `flags` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`day`, `skilltype`));