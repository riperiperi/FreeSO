-- claims

ALTER TABLE `fso_avatar_claims` 
DROP FOREIGN KEY `FK_fso_avatar_claims_fso_avatars`;
ALTER TABLE `fso_avatar_claims` 
ADD CONSTRAINT `FK_fso_avatar_claims_fso_avatars`
  FOREIGN KEY (`avatar_id`)
  REFERENCES `fso_avatars` (`avatar_id`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;


-- bookmarks

ALTER TABLE `fso_bookmarks` 
DROP FOREIGN KEY `FK_fso_bookmarks_fso_avatars_src`,
DROP FOREIGN KEY `FK_fso_bookmarks_fso_avatars_target`;
ALTER TABLE `fso_bookmarks` 
ADD CONSTRAINT `FK_fso_bookmarks_fso_avatars_src`
  FOREIGN KEY (`avatar_id`)
  REFERENCES `fso_avatars` (`avatar_id`)
  ON DELETE CASCADE
  ON UPDATE CASCADE,
ADD CONSTRAINT `FK_fso_bookmarks_fso_avatars_target`
  FOREIGN KEY (`target_id`)
  REFERENCES `fso_avatars` (`avatar_id`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;


-- lot owners - now nullable. null owner lots are deleted.

ALTER TABLE `fso_lots` 
CHANGE COLUMN `owner_id` `owner_id` INT(10) UNSIGNED NULL ;