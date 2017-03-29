ALTER TABLE `fso_users` 
ADD COLUMN `client_id` VARCHAR(100) NOT NULL DEFAULT 0 AFTER `last_ip`;

ALTER TABLE `fso_ip_ban` 
ADD COLUMN `client_id` VARCHAR(100) NOT NULL DEFAULT '0' AFTER `end_date`;