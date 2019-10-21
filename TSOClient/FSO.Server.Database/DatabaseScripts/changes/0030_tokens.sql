CREATE TABLE `fso_object_attributes` (
  `object_id` INT UNSIGNED NOT NULL COMMENT 'ID of the object this attribute is for.',
  `index` INT UNSIGNED NOT NULL COMMENT 'Index of the Attribute.',
  `value` INT NOT NULL COMMENT 'Value of the Attribute. Ingame these can only be short size, but I\'ve left them full int here just in case we need to expand.',
  PRIMARY KEY (`object_id`, `index`),
  INDEX `fso_object_id_attr_idx` (`object_id` ASC),
  CONSTRAINT `fso_object_id_attr`
    FOREIGN KEY (`object_id`)
    REFERENCES `fso_objects` (`object_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Attributes for objects in the fso_objects table. These are used for \'token\' objects, special objects which can track values such as secondary currencies and car keys (pointing to the car you are driving, for example)\nThese are only saved for objects of type token - other objects use filesystem objects to store attributes.';

ALTER TABLE `fso_objects` 
ADD COLUMN `has_db_attributes` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'If >0, attributes should be fetched from fso_object_attributes rather than trusting NFS state. 2 indicates value token.' AFTER `upgrade_level`;