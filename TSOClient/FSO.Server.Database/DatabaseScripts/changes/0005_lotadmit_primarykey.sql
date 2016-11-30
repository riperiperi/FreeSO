ALTER TABLE `fso_lot_admit` 
DROP PRIMARY KEY,
ADD PRIMARY KEY (`avatar_id`, `lot_id`, `admit_type`);
