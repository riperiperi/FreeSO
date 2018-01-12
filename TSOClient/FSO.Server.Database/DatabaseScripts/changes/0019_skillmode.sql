ALTER TABLE `fso_lots` 
ADD COLUMN `skill_mode` TINYINT UNSIGNED NOT NULL DEFAULT '0' AFTER `move_flags`;

UPDATE fso_lots SET skill_mode = 1 where category IN ("services", "entertainment", "romance");