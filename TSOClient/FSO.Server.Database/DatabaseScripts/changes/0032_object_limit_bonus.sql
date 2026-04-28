ALTER TABLE `fso_lots`
ADD COLUMN `object_limit_bonus` INT NOT NULL DEFAULT 0 AFTER `move_flags`;
