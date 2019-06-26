CREATE TABLE `fso_election_cycles` (
  `cycle_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `start_date` INT(10) UNSIGNED NOT NULL,
  `end_date` INT(10) UNSIGNED NOT NULL,
  `current_state` ENUM('shutdown', 'nomination', 'election', 'ended') NOT NULL,
  `election_type` ENUM('election', 'shutdown') NOT NULL,
  PRIMARY KEY (`cycle_id`));

CREATE TABLE `fso_neighborhoods` (
  `neighborhood_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(100) NOT NULL,
  `description` VARCHAR(1000) NOT NULL DEFAULT '',
  `shard_id` INT(11) NOT NULL,
  `location` INT(10) ZEROFILL UNSIGNED NOT NULL,
  `color` INT UNSIGNED NOT NULL DEFAULT 0xffffffff,
  `flag` INT UNSIGNED NOT NULL DEFAULT 0,
  `town_hall_id` INT(11) NULL,
  `icon_url` VARCHAR(100) NULL,
  `guid` VARCHAR(45) NOT NULL,
  `mayor_id` INT(10) UNSIGNED NULL,
  `mayor_elected_date` INT(10) UNSIGNED NOT NULL DEFAULT 0,
  `election_cycle_id` INT(10) UNSIGNED NULL,
  PRIMARY KEY (`neighborhood_id`),
  UNIQUE INDEX `town_hall_id_UNIQUE` (`town_hall_id` ASC),
  UNIQUE INDEX `guid_UNIQUE` (`guid` ASC),
  UNIQUE INDEX `mayor_id_UNIQUE` (`mayor_id` ASC),
  INDEX `fso_neigh_cycle_idx` (`election_cycle_id` ASC),
  INDEX `fso_neigh_shard_idx` (`shard_id` ASC),
  CONSTRAINT `fso_neigh_town_hall`
    FOREIGN KEY (`town_hall_id`)
    REFERENCES `fso_lots` (`lot_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_neigh_mayor`
    FOREIGN KEY (`mayor_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_neigh_cycle`
    FOREIGN KEY (`election_cycle_id`)
    REFERENCES `fso_election_cycles` (`cycle_id`)
    ON DELETE RESTRICT
    ON UPDATE CASCADE,
  CONSTRAINT `fso_neigh_shard`
    FOREIGN KEY (`shard_id`)
    REFERENCES `fso_shards` (`shard_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Neighborhoods within each shard. Typically created ingame and then imported using `server import_nhood <shard_id> <file>`.';

CREATE TABLE `fso_election_votes` (
  `election_cycle_id` INT(10) UNSIGNED NOT NULL,
  `from_avatar_id` INT(10) UNSIGNED NOT NULL,
  `type` ENUM('vote', 'nomination') NOT NULL,
  `target_avatar_id` INT(10) UNSIGNED NOT NULL,
  `date` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`election_cycle_id`, `from_avatar_id`, `type`),
  INDEX `fso_vote_from_idx` (`from_avatar_id` ASC),
  INDEX `fso_vote_to_idx` (`target_avatar_id` ASC),
  CONSTRAINT `fso_evote_cycle`
    FOREIGN KEY (`election_cycle_id`)
    REFERENCES `fso_election_cycles` (`cycle_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_evote_from`
    FOREIGN KEY (`from_avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_evote_to`
    FOREIGN KEY (`target_avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Votes and Nominations for mayor election cycles. You can get the neighborhood id from the election cycle.';

CREATE TABLE `fso_election_candidates` (
  `election_cycle_id` INT UNSIGNED NOT NULL,
  `candidate_avatar_id` INT UNSIGNED NOT NULL,
  `comment` VARCHAR(200) NULL DEFAULT 'No comment.',
  `disqualified` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Boolean. True if this mayor has been disqualified for some reason.',
  PRIMARY KEY (`election_cycle_id`, `candidate_avatar_id`),
  INDEX `fso_candidate_avatar_idx` (`candidate_avatar_id` ASC),
  CONSTRAINT `fso_candidate_cycle`
    FOREIGN KEY (`election_cycle_id`)
    REFERENCES `fso_election_cycles` (`cycle_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_candidate_avatar`
    FOREIGN KEY (`candidate_avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Candidates for an election cycle. You can get the neighborhood id from the election cycle.';

CREATE TABLE `fso_mayor_ratings` (
  `rating_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `from_user_id` INT UNSIGNED NOT NULL,
  `to_user_id` INT UNSIGNED NOT NULL,
  `rating` INT UNSIGNED NOT NULL DEFAULT 0,
  `comment` VARCHAR(200) NOT NULL DEFAULT 'No Comment.',
  `date` INT UNSIGNED NOT NULL DEFAULT 0,
  `from_avatar_id` INT UNSIGNED NULL,
  `to_avatar_id` INT UNSIGNED NULL,
  `anonymous` TINYINT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (`rating_id`),
  INDEX `fso_mrating_to_idx` (`to_user_id` ASC),
  INDEX `fso_mrating_from_idx` (`from_user_id` ASC),
  INDEX `fso_mrating_froma_idx` (`from_avatar_id` ASC),
  INDEX `fso_mrating_toa_idx` (`to_avatar_id` ASC),
  CONSTRAINT `fso_mrating_from`
    FOREIGN KEY (`from_user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_mrating_to`
    FOREIGN KEY (`to_user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_mrating_froma`
    FOREIGN KEY (`from_avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
  CONSTRAINT `fso_mrating_toa`
    FOREIGN KEY (`to_avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE SET NULL
    ON UPDATE CASCADE)
COMMENT = 'User ID is used for ratings so people can\'t dodge their reviews by using another avatar or creating another account. Avatar IDs are still stored for possible visual reference, though the visual reference can be nulled if the avatar is deleted.';

CREATE TABLE `fso_bulletin_posts` (
  `bulletin_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
  `neighborhood_id` INT NOT NULL,
  `avatar_id` INT UNSIGNED NOT NULL,
  `title` VARCHAR(64) NOT NULL,
  `body` VARCHAR(1000) NOT NULL,
  `date` INT UNSIGNED NOT NULL,
  `flags` INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`bulletin_id`),
  INDEX `fso_bulletin_neigh_idx` (`neighborhood_id` ASC),
  INDEX `fso_bulletin_poster_idx` (`avatar_id` ASC),
  CONSTRAINT `fso_bulletin_neigh`
    FOREIGN KEY (`neighborhood_id`)
    REFERENCES `fso_neighborhoods` (`neighborhood_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_bulletin_poster`
    FOREIGN KEY (`avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Posts for neighborhood bulletin boards. Can only be made by avatars that live in that neighborhood.';

ALTER TABLE `fso_avatars` 
ADD COLUMN `custom_guid` INT UNSIGNED NULL AFTER `moderation_level`,
ADD COLUMN `move_date` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `custom_guid`,
ADD COLUMN `name_date` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `move_date`,
ADD COLUMN `mayor_nhood` INT NULL AFTER `name_date`,
ADD INDEX `FK_avatar_mayor_idx` (`mayor_nhood` ASC);
ALTER TABLE `fso_avatars` 
ADD CONSTRAINT `FK_avatar_mayor`
  FOREIGN KEY (`mayor_nhood`)
  REFERENCES `fso_neighborhoods` (`neighborhood_id`)
  ON DELETE SET NULL
  ON UPDATE CASCADE;

ALTER TABLE `fso_mayor_ratings` 
ADD COLUMN `neighborhood` INT(10) UNSIGNED NOT NULL AFTER `anonymous`;

ALTER TABLE `fso_lots` 
CHANGE COLUMN `category` `category` ENUM('none', 'welcome', 'money', 'skills', 'services', 'entertainment', 'romance', 'shopping', 'games', 'offbeat', 'residence', 'community') NOT NULL ;

CREATE TABLE `fso_nhood_ban` (
  `user_id` INT UNSIGNED NOT NULL,
  `ban_reason` VARCHAR(1000) NOT NULL DEFAULT 'You have been banned from using neighborhood gameplay for misuse. For more information, see http://freeso.org/nhoodrules/',
  `end_date` INT UNSIGNED NOT NULL,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `fso_nhood_ban_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `fso_users` (`user_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Table managing users who have been banned from Neighborhood Gameplay. (eg. inappropriate ratings, bribery in mayor campaign, abuse of bulletin board)';

ALTER TABLE `fso_events` 
CHANGE COLUMN `description` `description` VARCHAR(1500) NULL DEFAULT NULL ,
CHANGE COLUMN `mail_message` `mail_message` VARCHAR(1500) NULL DEFAULT NULL ;

CREATE TRIGGER `fso_election_votes_BEFORE_INSERT` BEFORE INSERT ON `fso_election_votes` FOR EACH ROW BEGIN
 IF (SELECT COUNT(*) from fso_election_votes v INNER JOIN fso_avatars va ON v.from_avatar_id = va.avatar_id
  WHERE v.election_cycle_id = @cycle_id AND v.type = @type AND va.user_id IN
   (SELECT user_id FROM fso_users WHERE last_ip =
    (SELECT last_ip FROM fso_avatars a JOIN fso_users u on a.user_id = u.user_id WHERE avatar_id = @avatar_id)
   )) > 0 THEN
  SIGNAL SQLSTATE '45000'
  SET MESSAGE_TEXT = 'A vote from this person or someone related already exists for this cycle.';
 END IF;
END;

ALTER TABLE `fso_mayor_ratings` 
ADD UNIQUE INDEX `fso_rating_user_to_ava` (`from_user_id` ASC, `to_avatar_id` ASC);

ALTER TABLE `fso_tasks` 
CHANGE COLUMN `task_type` `task_type` ENUM('prune_database', 'bonus', 'shutdown', 'job_balance', 'multi_check', 'prune_abandoned_lots', 'neighborhood_tick') NOT NULL ;

CREATE TABLE `fso_election_cyclemail` (
  `avatar_id` INT UNSIGNED NOT NULL,
  `cycle_id` INT UNSIGNED NOT NULL,
  `cycle_state` ENUM('shutdown', 'nomination', 'election', 'ended', 'failsafe') NOT NULL,
  PRIMARY KEY (`avatar_id`, `cycle_id`, `cycle_state`),
  INDEX `fso_cmail_cycle_idx` (`cycle_id` ASC),
  CONSTRAINT `fso_cmail_ava`
    FOREIGN KEY (`avatar_id`)
    REFERENCES `fso_avatars` (`avatar_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `fso_cmail_cycle`
    FOREIGN KEY (`cycle_id`)
    REFERENCES `fso_election_cycles` (`cycle_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE);

ALTER TABLE `fso_election_cycles` 
CHANGE COLUMN `current_state` `current_state` ENUM('shutdown', 'nomination', 'election', 'ended', 'failsafe') NOT NULL ;

ALTER TABLE `fso_neighborhoods` 
DROP FOREIGN KEY `fso_neigh_cycle`,
DROP FOREIGN KEY `fso_neigh_mayor`,
DROP FOREIGN KEY `fso_neigh_town_hall`;
ALTER TABLE `fso_neighborhoods` 
ADD CONSTRAINT `fso_neigh_cycle`
  FOREIGN KEY (`election_cycle_id`)
  REFERENCES `fso_election_cycles` (`cycle_id`)
  ON DELETE SET NULL
  ON UPDATE CASCADE,
ADD CONSTRAINT `fso_neigh_mayor`
  FOREIGN KEY (`mayor_id`)
  REFERENCES `fso_avatars` (`avatar_id`)
  ON DELETE SET NULL
  ON UPDATE CASCADE,
ADD CONSTRAINT `fso_neigh_town_hall`
  FOREIGN KEY (`town_hall_id`)
  REFERENCES `fso_lots` (`lot_id`)
  ON DELETE SET NULL
  ON UPDATE CASCADE;

ALTER TABLE `fso_election_candidates` 
CHANGE COLUMN `disqualified` `state` ENUM('informed', 'running', 'disqualified', 'lost', 'won') NOT NULL DEFAULT 'informed' COMMENT 'Boolean. True if this mayor has been disqualified for some reason.' ;

ALTER TABLE `fso_election_cycles` 
ADD COLUMN `neighborhood_id` INT NULL AFTER `election_type`,
ADD INDEX `fso_cycle_nhood_idx` (`neighborhood_id` ASC);
ALTER TABLE `fso_election_cycles` 
ADD CONSTRAINT `fso_cycle_nhood`
  FOREIGN KEY (`neighborhood_id`)
  REFERENCES `fso_neighborhoods` (`neighborhood_id`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;