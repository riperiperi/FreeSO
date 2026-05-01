-- Adds the purchasable skill-lock points bonus. The portal lets avatar owners buy
-- additional points to spend on skill locks beyond what their age normally grants.
-- Avatar_SkillsLockPoints (computed) = 20 + (Avatar_Age / 7) + skilllock_bonus.
ALTER TABLE `fso_avatars`
ADD COLUMN `skilllock_bonus` INT UNSIGNED NOT NULL DEFAULT 0 AFTER `lock_creativity`;
