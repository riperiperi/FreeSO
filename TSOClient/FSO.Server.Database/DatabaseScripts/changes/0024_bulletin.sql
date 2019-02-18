ALTER TABLE `fso_bulletin_posts` 
DROP FOREIGN KEY `fso_bulletin_poster`;
ALTER TABLE `fso_bulletin_posts` 
CHANGE COLUMN `avatar_id` `avatar_id` INT(10) UNSIGNED NULL ,
ADD COLUMN `lot_id` INT(11) NULL AFTER `flags`,
ADD COLUMN `type` ENUM('mayor', 'system', 'community') NULL AFTER `lot_id`,
ADD INDEX `fso_bulletin_lot_idx` (`lot_id` ASC);
ALTER TABLE `fso_bulletin_posts` 
ADD CONSTRAINT `fso_bulletin_poster`
  FOREIGN KEY (`avatar_id`)
  REFERENCES `fso_avatars` (`avatar_id`)
  ON DELETE CASCADE
  ON UPDATE CASCADE,
ADD CONSTRAINT `fso_bulletin_lot`
  FOREIGN KEY (`lot_id`)
  REFERENCES `fso_lots` (`lot_id`)
  ON DELETE SET NULL
  ON UPDATE CASCADE;

ALTER TABLE `fso_bulletin_posts` 
DROP FOREIGN KEY `fso_bulletin_lot`;
ALTER TABLE `fso_bulletin_posts` 
CHANGE COLUMN `lot_id` `lot_id` INT(11) UNSIGNED NULL DEFAULT NULL ,
ADD INDEX `fso_bulletin_lot_idx` (`lot_id` ASC),
DROP INDEX `fso_bulletin_lot_idx` ;
ALTER TABLE `fso_bulletin_posts` 
ADD CONSTRAINT `fso_bulletin_lot`
  FOREIGN KEY (`lot_id`)
  REFERENCES `fso_lots` (`location`)
  ON DELETE SET NULL
  ON UPDATE CASCADE;

ALTER TABLE `fso_bulletin_posts` 
ADD COLUMN `deleted` TINYINT UNSIGNED NOT NULL DEFAULT 0 AFTER `type`;