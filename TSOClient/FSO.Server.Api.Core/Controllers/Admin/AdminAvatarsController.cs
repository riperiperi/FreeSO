using System;
using System.Net;
using FSO.Server.Common;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Protocol.Gluon.Packets;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/avatars")]
    [ApiController]
    public class AdminAvatarsController : ControllerBase
    {
        // Hard cap on purchasable skilllock_bonus. The in-game lock-setting code rejects
        // any per-skill lock above the avatar's actual skill level (max 20 per skill, 6
        // skills → 120 total possible locks). With the natural 20+Age/7 pool starting at
        // 20, capping the purchasable bonus at 100 lets any avatar reach the 120 ceiling
        // without wasted simoleons.
        private const int BonusMax = 100;

        // Pricing curve (matches the formula chosen for portal):
        //   marginal cost of step N = 10,000 × 1.05^(N-1)
        //   cumulative cost to reach step N = 200,000 × (1.05^N − 1)
        // Step 1 = $10,000 ; step 30 ≈ $664k ; step 100 ≈ $26M.
        // KEEP IN SYNC with the table in freeso-portal routes/user.py.
        private static int CumulativeCost(int step)
        {
            if (step <= 0) return 0;
            double v = 200_000.0 * (Math.Pow(1.05, step) - 1.0);
            // Round to nearest int; clamp to int range defensively (step 100 ≈ 2.6e7,
            // well under int.MaxValue, but step ~150 would overflow ushort costs).
            if (v > int.MaxValue) return int.MaxValue;
            return (int)Math.Round(v);
        }

        public class SkillLockBonusPurchaseRequest
        {
            public int target_bonus { get; set; }
        }

        /// <summary>
        /// Atomic skill-lock-points bonus purchase. Used by the freeso-portal so avatar
        /// owners can buy additional Avatar_SkillsLockPoints beyond the natural
        /// age-derived pool. Performs the budget debit and the bonus update in a single
        /// SQL transaction (with row locks).
        ///
        /// No live update: skill-lock pool size is read on next avatar load. Locks can
        /// only be modified off-lot anyway, so a stale in-memory value won't corrupt
        /// state — it'll just be slightly behind until the avatar reconnects.
        ///
        /// Caller (the portal) authenticates via the existing admin oauth bearer token.
        /// The portal verifies user→avatar ownership before calling; this endpoint does
        /// not re-check the user binding (no user_id in the request body), only that
        /// the avatar exists, has enough money, and the target is an upgrade.
        /// </summary>
        [HttpPost("{avatarId}/skilllock_bonus_purchase")]
        public IActionResult PurchaseSkillLockBonus(uint avatarId, [FromBody] SkillLockBonusPurchaseRequest body)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            if (body == null) return BadRequest(new { error = "missing_body" });

            // Clamp/normalize.
            var target = body.target_bonus;
            if (target < 0) target = 0;
            if (target > BonusMax) target = BonusMax;

            DbSkillLockBonusPurchase result;
            uint avatarAgeDays;
            using (var da = api.DAFactory.Get())
            {
                var avatar = da.Avatars.Get(avatarId);
                if (avatar == null) return NotFound(new { error = "avatar_not_found" });

                int fromStep = (int)avatar.skilllock_bonus;
                int cost = CumulativeCost(target) - CumulativeCost(fromStep);
                if (cost < 0) cost = 0;

                // Capture age now so we can compute the new SkillLocks limit for the live
                // broadcast. Matches the calc in LotContainer.BuildAvatarState (line 1422).
                var now = Epoch.Now;
                avatarAgeDays = (uint)((now - avatar.date) / ((long)60 * 60 * 24));

                result = da.Avatars.PurchaseSkillLockBonus(avatarId, (uint)target, cost);
            }

            switch (result.Status)
            {
                case SkillLockBonusPurchaseStatus.Success:
                    break;
                case SkillLockBonusPurchaseStatus.AvatarNotFound:
                    return NotFound(new { error = "avatar_not_found" });
                case SkillLockBonusPurchaseStatus.NotAnUpgrade:
                    return BadRequest(new { error = "not_an_upgrade" });
                case SkillLockBonusPurchaseStatus.InsufficientFunds:
                    return BadRequest(new { error = "insufficient_funds" });
                case SkillLockBonusPurchaseStatus.DbError:
                default:
                    return StatusCode(500, new { error = "db_error" });
            }

            // City-side: invalidate the cached Avatar model so the next client poll
            // (PollTopics, ~5s cadence) re-loads from MySQL and the in-game UI reflects
            // the new Avatar_SkillsLockPoints. No-op on lot/task hosts.
            api.InvalidateAvatar(avatarId);

            // Live update: tell every lot server the new SkillLocks pool size for this
            // avatar. Each server walks its hosted lots; the one (zero or one) that has
            // the avatar pushes the change into the live VM via VMNetSetAvatarSkillLocksCmd.
            // If the avatar isn't on a lot anywhere, the next BuildAvatarState (on next
            // lot join) reads the new value from the DB.
            int notified = 0;
            if (api.HostPool != null)
            {
                int newLimit = 20 + (int)(avatarAgeDays / 7) + (int)result.NewBonus;
                if (newLimit > short.MaxValue) newLimit = short.MaxValue;
                var packet = new SetAvatarSkillLockLimit
                {
                    AvatarId = avatarId,
                    NewLimit = (short)newLimit,
                };
                foreach (var host in api.HostPool.GetByRole(DbHostRole.lot))
                {
                    if (host.Connected)
                    {
                        host.Write(packet);
                        notified++;
                    }
                }
            }

            return new JsonResult(new
            {
                ok = true,
                avatar_id = avatarId,
                new_bonus = result.NewBonus,
                cost_charged = result.CostCharged,
                new_budget = result.NewBudget,
                lot_servers_notified = notified,
            });
        }
    }
}