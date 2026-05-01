namespace FSO.Server.Database.DA.Lots
{
    /// <summary>
    /// Result of an atomic ObjectLimitBonus purchase. Status differentiates the failure
    /// modes the API surfaces back to the portal so the UI can show a clear message
    /// without parsing exception strings.
    /// </summary>
    public class DbObjectLimitBonusPurchase
    {
        public ObjectLimitBonusPurchaseStatus Status { get; set; }
        /// <summary>The avatar's budget AFTER the purchase (only meaningful on Success).</summary>
        public int NewBudget { get; set; }
        /// <summary>The lot's bonus AFTER the purchase (only meaningful on Success).</summary>
        public int NewBonus { get; set; }
        /// <summary>The simoleons charged (only meaningful on Success).</summary>
        public int CostCharged { get; set; }
    }

    public enum ObjectLimitBonusPurchaseStatus
    {
        Success = 0,
        LotNotFound = 1,
        AvatarNotFound = 2,
        NotLotOwner = 3,
        NotAnUpgrade = 4,      // target_bonus <= current
        InsufficientFunds = 5,
        DbError = 6,
    }
}