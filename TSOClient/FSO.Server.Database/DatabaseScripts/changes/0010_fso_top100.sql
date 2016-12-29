CREATE TABLE IF NOT EXISTS `fso_lot_visit_totals` (
  `lot_id` int(11) NOT NULL,
  `date` date NOT NULL,
  `minutes` int(11) NOT NULL,
  PRIMARY KEY (`lot_id`,`date`),
  CONSTRAINT `FK_fso_lot_visit_totals_fso_lots` FOREIGN KEY (`lot_id`) REFERENCES `fso_lots` (`lot_id`) ON DELETE CASCADE ON UPDATE CASCADE
) COLLATE='utf8_general_ci'
  ENGINE=InnoDB;


 CREATE PROCEDURE `fso_lot_top_100_calc_category`(IN `p_category` VARCHAR(50), IN `p_date` DATE, IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	
	DECLARE EXIT HANDLER FOR SQLEXCEPTION 
    BEGIN
         ROLLBACK;
    END;
	
	SET @date = p_date;
	SET @start_date = DATE_SUB(@date, INTERVAL 4 DAY);
	SET @timestamp = current_timestamp;
	SET @row_number = 0;
	
	START TRANSACTION;
		# Remove old
		DELETE FROM fso_lot_top_100 WHERE shard_id = p_shard_id AND category = p_category;
		
		
		# Insert new
		INSERT INTO fso_lot_top_100 (category, rank, shard_id, lot_id, minutes, date)
			SELECT category, 
					(@row_number:=@row_number + 1) AS rank, 
					shard_id, 
					lot_id, 
					minutes, 
					date 
					FROM (
						SELECT lot.category, lot.lot_id, lot.shard_id, FLOOR(AVG(visits.minutes)) as minutes, @timestamp as date 
							FROM fso_lot_visit_totals visits 
								INNER JOIN fso_lots lot ON visits.lot_id = lot.lot_id
							WHERE lot.category = p_category 
								AND date BETWEEN @start_date AND @date
								AND lot.shard_id = p_shard_id
							GROUP BY lot_id
							ORDER BY minutes DESC
							LIMIT 100
					) as top100;
	COMMIT;
END;

CREATE PROCEDURE `fso_lot_top_100_calc_all`(IN `p_date` DATE, IN `p_shard_id` INT)
    SQL SECURITY INVOKER
BEGIN
	CALL fso_lot_top_100_calc_category('money', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('offbeat', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('romance', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('services', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('shopping', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('skills', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('welcome', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('games', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('entertainment', p_date, p_shard_id);
	CALL fso_lot_top_100_calc_category('residence', p_date, p_shard_id);
END;