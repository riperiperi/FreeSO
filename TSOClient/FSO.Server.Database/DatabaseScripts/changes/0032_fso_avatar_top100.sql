CREATE TABLE IF NOT EXISTS `fso_avatar_top_100` (
  `category` enum('most_famous','best_karma','friendliest','most_infamous','meanest') NOT NULL,
  `rank` tinyint(4) unsigned NOT NULL,
  `shard_id` int(11) NOT NULL,
  `avatar_id` int(10) unsigned DEFAULT NULL,
  `value` int(11) DEFAULT NULL,
  `date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`category`,`rank`),
  KEY `FK_fso_avatar_top_100_fso_shards` (`shard_id`),
  KEY `FK_fso_avatar_top_100_fso_avatars` (`avatar_id`),
  CONSTRAINT `FK_fso_avatar_top_100_fso_avatars` FOREIGN KEY (`avatar_id`) REFERENCES `fso_avatars` (`avatar_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_fso_avatar_top_100_fso_shards` FOREIGN KEY (`shard_id`) REFERENCES `fso_shards` (`shard_id`)
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;

DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_most_famous`;


CREATE PROCEDURE `fso_avatar_top_100_calc_most_famous`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	SET @timestamp = current_timestamp;
	SET @row_number = 0;

	START TRANSACTION;
		# Remove old
		DELETE FROM fso_avatar_top_100 WHERE shard_id = p_shard_id AND category = 'most_famous';
		
		# Insert new
		INSERT INTO fso_avatar_top_100 (category, rank, shard_id, avatar_id, value, date)
			SELECT 'most_famous' AS category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					avatar_id, 
					value, 
					@timestamp AS date
					FROM (
						SELECT avatar.shard_id, avatar.avatar_id, COUNT(*) as value
						FROM fso_relationships relationship
							INNER JOIN fso_avatars avatar ON relationship.to_id = avatar.avatar_id
						WHERE avatar.shard_id = p_shard_id AND relationship.index = 1 AND relationship.value >= 60
						GROUP BY avatar.avatar_id
						ORDER BY COUNT(*) DESC
						LIMIT 100
					)
	COMMIT;
END;



DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_best_karma`;


CREATE PROCEDURE `fso_avatar_top_100_calc_best_karma`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	SET @timestamp = current_timestamp;
	SET @row_number = 0;

	START TRANSACTION;
		# Remove old
		DELETE FROM fso_avatar_top_100 WHERE shard_id = p_shard_id AND category = 'best_karma';
		
		# Insert new
		INSERT INTO fso_avatar_top_100 (category, rank, shard_id, avatar_id, value, date)
			SELECT 'best_karma' AS category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					avatar_id, 
					value, 
					@timestamp AS date
					FROM (
						SELECT avatar.shard_id, avatar.avatar_id, SUM(value) as value
						FROM fso_relationships relationship
							INNER JOIN fso_avatars avatar ON relationship.from_id = avatar.avatar_id
						WHERE avatar.shard_id = p_shard_id AND relationship.index = 1
						GROUP BY avatar.avatar_id
						ORDER BY SUM(value) DESC
						LIMIT 100
					)
	COMMIT;
END;


DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_friendliest`;


CREATE PROCEDURE `fso_avatar_top_100_calc_friendliest`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	SET @timestamp = current_timestamp;
	SET @row_number = 0;

	START TRANSACTION;
		# Remove old
		DELETE FROM fso_avatar_top_100 WHERE shard_id = p_shard_id AND category = 'friendliest';
		
		# Insert new
		INSERT INTO fso_avatar_top_100 (category, rank, shard_id, avatar_id, value, date)
			SELECT 'friendliest' AS category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					avatar_id, 
					value, 
					@timestamp AS date
					FROM (
						SELECT avatar.shard_id, avatar.avatar_id, COUNT(*) as value
						FROM fso_relationships relationship
							INNER JOIN fso_avatars avatar ON relationship.from_id = avatar.avatar_id
						WHERE avatar.shard_id = p_shard_id AND relationship.index = 1 AND relationship.value >= 60
						GROUP BY avatar.avatar_id
						ORDER BY COUNT(*) DESC
						LIMIT 100
					)
	COMMIT;
END;



DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_most_infamous`;


CREATE PROCEDURE `fso_avatar_top_100_calc_most_infamous`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	SET @timestamp = current_timestamp;
	SET @row_number = 0;

	START TRANSACTION;
		# Remove old
		DELETE FROM fso_avatar_top_100 WHERE shard_id = p_shard_id AND category = 'most_infamous';
		
		# Insert new
		INSERT INTO fso_avatar_top_100 (category, rank, shard_id, avatar_id, value, date)
			SELECT 'most_infamous' AS category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					avatar_id, 
					value, 
					@timestamp AS date
					FROM (
						SELECT avatar.shard_id, avatar.avatar_id, COUNT(*) as value
						FROM fso_relationships relationship
							INNER JOIN fso_avatars avatar ON relationship.to_id = avatar.avatar_id
						WHERE avatar.shard_id = p_shard_id AND relationship.index = 1 AND relationship.value <= -60
						GROUP BY avatar.avatar_id
						ORDER BY COUNT(*) DESC
						LIMIT 100
					)
	COMMIT;
END;



DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_meanest`;


CREATE PROCEDURE `fso_avatar_top_100_calc_meanest`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	SET @timestamp = current_timestamp;
	SET @row_number = 0;

	START TRANSACTION;
		# Remove old
		DELETE FROM fso_avatar_top_100 WHERE shard_id = p_shard_id AND category = 'meanest';
		
		# Insert new
		INSERT INTO fso_avatar_top_100 (category, rank, shard_id, avatar_id, value, date)
			SELECT 'meanest' AS category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					avatar_id, 
					value, 
					@timestamp AS date
					FROM (
						SELECT avatar.shard_id, avatar.avatar_id, COUNT(*) as value
						FROM fso_relationships relationship
							INNER JOIN fso_avatars avatar ON relationship.from_id = avatar.avatar_id
						WHERE avatar.shard_id = p_shard_id AND relationship.index = 1 AND relationship.value <= -60
						GROUP BY avatar.avatar_id
						ORDER BY COUNT(*) DESC
						LIMIT 100
					)
	COMMIT;
END;


DROP PROCEDURE IF EXISTS `fso_avatar_top_100_calc_all`;


CREATE PROCEDURE `fso_avatar_top_100_calc_all`(IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	CALL fso_avatar_top_100_calc_most_famous(p_shard_id);
	CALL fso_avatar_top_100_calc_best_karma(p_shard_id);
	CALL fso_avatar_top_100_calc_friendliest(p_shard_id);
	CALL fso_avatar_top_100_calc_most_infamous(p_shard_id);
	CALL fso_avatar_top_100_calc_meanest(p_shard_id);
END;