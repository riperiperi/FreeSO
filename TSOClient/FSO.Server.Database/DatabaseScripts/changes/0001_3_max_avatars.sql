CREATE TRIGGER `fso_avatars_BEFORE_INSERT` BEFORE INSERT ON `fso_avatars` FOR EACH ROW BEGIN
 IF (SELECT COUNT(*) FROM fso_avatars a WHERE NEW.user_id = a.user_id) >= 3 THEN
  SIGNAL SQLSTATE '45000'
  SET MESSAGE_TEXT = 'Cannot own more than 3 avatars.';
 END IF;
END