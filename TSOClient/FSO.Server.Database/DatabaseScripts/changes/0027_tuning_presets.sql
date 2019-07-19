CREATE TABLE `fso_tuning_presets` (
  `preset_id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(128) NULL,
  `description` VARCHAR(1000) NULL,
  `flags` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`preset_id`))
COMMENT = 'Presets are collections of tuning info that can be applied and removed at the same time as part of an event.';

CREATE TABLE `fso_tuning_preset_items` (
  `item_id` INT NOT NULL AUTO_INCREMENT,
  `preset_id` INT NOT NULL,
  `tuning_type` VARCHAR(128) NOT NULL,
  `tuning_table` INT NOT NULL,
  `tuning_index` INT NOT NULL,
  `value` FLOAT NOT NULL,
  PRIMARY KEY (`item_id`),
  INDEX `fso_preset_item_to_preset_fk_idx` (`preset_id` ASC),
  CONSTRAINT `fso_preset_item_to_preset_fk`
    FOREIGN KEY (`preset_id`)
    REFERENCES `fso_tuning_presets` (`preset_id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE)
COMMENT = 'Individual tuning items that belong to a preset configuration, to be applied as a group. Similar to fso_tuning, built to copy right into it.';