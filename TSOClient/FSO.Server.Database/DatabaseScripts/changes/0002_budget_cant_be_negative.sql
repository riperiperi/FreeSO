CREATE TRIGGER `fso_avatars_BEFORE_UPDATE` BEFORE UPDATE ON `fso_avatars` FOR EACH ROW BEGIN
    IF NEW.budget<0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Transaction would cause avatar to have negative budget.';
    END IF;
END;

CREATE TRIGGER `fso_objects_BEFORE_UPDATE` BEFORE UPDATE ON `fso_objects` FOR EACH ROW BEGIN
    IF NEW.budget<0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Transaction would cause object to have negative budget.';
    END IF;
END;