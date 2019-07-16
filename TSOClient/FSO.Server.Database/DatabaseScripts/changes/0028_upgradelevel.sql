ALTER TABLE `fso_objects` 
ADD COLUMN `upgrade_level` INT(11) UNSIGNED NOT NULL DEFAULT 0 AFTER `dyn_flags_2`;