﻿ALTER TABLE `fso_users` 
ADD COLUMN `last_login` INT(10) UNSIGNED NOT NULL DEFAULT 0 AFTER `client_id`;