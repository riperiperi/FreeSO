CREATE TRIGGER `fso_roommates_BEFORE_INSERT` BEFORE INSERT ON `fso_roommates` FOR EACH ROW BEGIN
 IF (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.avatar_id = a.avatar_id) > 0 THEN
  SIGNAL SQLSTATE '45000'
  SET MESSAGE_TEXT = 'Cannot be a roommate of more than one lot. (currently, will likely change in future.)';
 END IF;
 IF (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.lot_id = a.lot_id) >= 8 THEN
  SIGNAL SQLSTATE '45000'
  SET MESSAGE_TEXT = 'Cannot have more than 8 roommates in a lot.';
 END IF;
END;