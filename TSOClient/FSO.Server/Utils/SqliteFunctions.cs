namespace FSO.Server.Utils
{
    /**
     * Triggers and functions for the database when running on sqlite.
     **/
    internal static class SqliteFunctions
    {
        public static string AvatarCountLimitTrigger = @"CREATE TRIGGER `fso_avatars_BEFORE_INSERT` BEFORE INSERT ON `fso_avatars` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN (SELECT COUNT(*) FROM fso_avatars a WHERE NEW.user_id = a.user_id) >= 3 THEN
    RAISE (ABORT, 'Cannot own more than 3 avatars.')
  END;
END;";


        public static string AvatarBudgetNegativeTrigger = @"CREATE TRIGGER `fso_avatars_BEFORE_UPDATE` BEFORE UPDATE ON `fso_avatars` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN NEW.budget<0 THEN
    RAISE (ABORT, 'Transaction would cause avatar to have negative budget.')
  END;
END;";

        public static string ObjectBudgetNegativeTrigger = @"CREATE TRIGGER `fso_objects_BEFORE_UPDATE` BEFORE UPDATE ON `fso_objects` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN NEW.budget<0 THEN
    RAISE (ABORT, 'Transaction would cause object to have negative budget.')
  END;
END;";

        public static string RoommateValidationTrigger = @"CREATE TRIGGER `fso_roommates_BEFORE_INSERT` BEFORE INSERT ON `fso_roommates` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.avatar_id = a.avatar_id) > 0 THEN
    RAISE (ABORT, 'Cannot be a roommate of more than one lot. (currently, will likely change in future.)')
  END;
 SELECT
  CASE
   WHEN (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.lot_id = a.lot_id) >= 8 THEN
    RAISE (ABORT, 'Cannot have more than 8 roommates in a lot.')
  END;
END;";


        public static string OutfitRackLimitTrigger = @"CREATE TRIGGER `fso_outfits_before_insert` BEFORE INSERT ON `fso_outfits` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN NEW.object_owner IS NOT NULL AND (SELECT COUNT(*) FROM fso_outfits o WHERE NEW.object_owner = o.object_owner) >= 20 THEN
    RAISE (ABORT, 'Cannot have more than 20 outfits in a rack.')
  END;
END;";

        public static string OutfitBackpackLimitTrigger = @"CREATE TRIGGER `fso_outfits_before_update` BEFORE UPDATE ON `fso_outfits` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN NEW.avatar_owner IS NOT NULL AND (SELECT COUNT(*) FROM fso_outfits o WHERE NEW.avatar_owner = o.avatar_owner AND o.outfit_type = NEW.outfit_type) >= 5 THEN
    RAISE (ABORT, 'Cannot have more than 5 outfits per category in backpack.')
  END;
END;";

        public static string BonusToAvatarTrigger = @"CREATE TRIGGER `fso_bonus_after_insert` AFTER INSERT ON `fso_bonus` FOR EACH ROW BEGIN
 UPDATE fso_avatars SET budget = (budget + IFNULL(NEW.bonus_visitor,0) + IFNULL(NEW.bonus_property,0) + IFNULL(NEW.bonus_sim,0)) WHERE avatar_id = NEW.avatar_id;
END;";

        // I don't know if this works.
        public static string SingleVoteTrigger = @"CREATE TRIGGER `fso_election_votes_BEFORE_INSERT` BEFORE INSERT ON `fso_election_votes` FOR EACH ROW BEGIN
 SELECT
  CASE
   WHEN (SELECT COUNT(*) from fso_election_votes v INNER JOIN fso_avatars va ON v.from_avatar_id = va.avatar_id
    WHERE v.election_cycle_id = NEW.election_cycle_id AND v.type = NEW.type AND va.user_id IN
     (SELECT user_id FROM fso_users WHERE last_ip =
      (SELECT last_ip FROM fso_avatars a JOIN fso_users u on a.user_id = u.user_id WHERE avatar_id = NEW.from_avatar_id)
     )) > 0 THEN
    RAISE (ABORT, 'A vote from this person or someone related already exists for this cycle.')
  END;
END;";

        public static string[] All =
        {
            AvatarCountLimitTrigger,
            AvatarBudgetNegativeTrigger,
            ObjectBudgetNegativeTrigger,
            RoommateValidationTrigger,
            OutfitRackLimitTrigger,
            OutfitBackpackLimitTrigger,
            BonusToAvatarTrigger,
            SingleVoteTrigger,
        };
    }
}
