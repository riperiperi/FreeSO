namespace FSO.Server.Protocol.Gluon.Model
{
    public enum ClaimAction
    {
        /// <summary>
        /// Not determined - likely going to host or spectate.
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Opens the lot normally.
        /// </summary>
        LOT_HOST,

        /// <summary>
        /// Opens the lot starting in spectator mode (saving is disabled).
        /// The lot will transition to regular host mode when a roommate joins.
        /// </summary>
        LOT_SPECTATOR,

        /// <summary>
        /// Open the lot and immediately save + close it.
        /// Removes objects that shouldn't be on a property and deletes it if there is no owner,
        /// applies terrain changes, and updates the hollow save for surrounding lots.
        /// </summary>
        LOT_CLEANUP,

        /// <summary>
        /// Opens the lot and immediately closes it, saving only the hollow.fsov used for surrounding lots.
        /// Move flags and ownership rules are applied to the lot for the hollow save, but not consumed.
        /// </summary>
        LOT_CLEANUP_HOLLOW
    }
    
    public static class ClaimActionExtensions
    {
        /// <summary>
        /// Check if the claim action is a cleanup type action. This means that the lot shouldn't expect anyone to join and should close as soon as possible.
        /// </summary>
        /// <param name="action">Claim action</param>
        /// <returns>True if the action is a cleanup type action, false otherwise</returns>
        public static bool IsCleanup(this ClaimAction action)
        {
            return action == ClaimAction.LOT_CLEANUP || action == ClaimAction.LOT_CLEANUP_HOLLOW;
        }
    }
}
