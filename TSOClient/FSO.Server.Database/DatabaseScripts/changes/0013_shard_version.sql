
ALTER TABLE `fso_shards`
ADD `version_name` varchar(100) NOT NULL DEFAULT 'unknown';

ALTER TABLE `fso_shards`
ADD `version_number` varchar(100) NOT NULL DEFAULT '0';
