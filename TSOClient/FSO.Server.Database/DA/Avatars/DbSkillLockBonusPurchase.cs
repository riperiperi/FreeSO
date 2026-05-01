namespace FSO.Server.Database.DA.Avatars
{
    /// <summary>
    /// Result of an atomic skill-lock-points bonus purchase. Status differentiates the
    /// failure modes the API surfaces back to the portal so the UI can show a clear
    /// message without parsing exception strings.
    /// </summary>
    public class DbSkillLockBonusPurchase
    {
        public SkillLockBonusPurchaseStatus Status { get; set; }
        /// <summary>The avatar's budget AFTER the purchase (only meaningful on Success).</summary>
        public int NewBudget { get; set; }
        /// <summary>The avatar's skilllock_bonus AFTER the purchase (only meaningful on Success).</summary>
        public uint NewBonus { get; set; }
        /// <summary>The simoleons charged (only meaningful on Success).</summary>
        public int CostCharged { get; set; }
    }

    public enum SkillLockBonusPurchaseStatus
    {
        Success = 0,
        AvatarNotFound = 1,
        NotAnUpgrade = 2,        // target_bonus <= current
        InsufficientFunds = 3,
        DbError = 4,
    }
}