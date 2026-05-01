using System.Net;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Protocol.Gluon.Packets;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FSO.Server.Api.Core.Controllers.Admin
{
    [EnableCors("AdminAppPolicy")]
    [Route("admin/lots")]
    [ApiController]
    public class AdminLotsController : ControllerBase
    {
        // KEEP IN SYNC with VMBuildableAreaInfo.ObjectLimitBonusPrices /
        // ObjectLimitBonusStep / ObjectLimitBonusMax in tso.simantics.
        // (We can't reference tso.simantics directly — it's net45, this is netcoreapp2.2.)
        // 26 entries: index = step (0 = no bonus, 25 = +500/roomie). Each value is the
        // cumulative simoleon cost to reach that step from 0.
        private static readonly int[] BonusPrices = new int[]
        {
                  0,    2000,    5000,    9000,   14000,   20000,   27000,   35000,
              44000,   54000,   65000,   77000,   90000,  104000,  119000,  135000,
             152000,  170000,  189000,  209000,  230000,  252000,  275000,  299000,
             324000,  350000
        };
        private const int BonusStep = 20;
        private const int BonusMax = 500;

        public class ObjectLimitBonusPurchaseRequest
        {
            public uint avatar_id { get; set; }
            public int target_bonus { get; set; }
        }

        /// <summary>
        /// Atomic ObjectLimitBonus purchase. Used by the freeso-portal to let lot owners
        /// buy extra object slots from outside the game. Performs the budget debit and
        /// the lot's bonus update in a single SQL transaction (with row locks), then
        /// broadcasts a SetLotObjectLimitBonus Gluon packet to every connected lot
        /// server so any in-progress lot host picks up the change in its live VM.
        ///
        /// Caller (the portal) authenticates via the existing admin oauth bearer token,
        /// so DemandModerator is sufficient — the per-lot ownership check is enforced
        /// inside the SQL transaction (avatar_id must equal lot.owner_id).
        /// </summary>
        [HttpPost("{lotId}/object_limit_bonus")]
        public IActionResult PurchaseObjectLimitBonus(int lotId, [FromBody] ObjectLimitBonusPurchaseRequest body)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            if (body == null) return BadRequest(new { error = "missing_body" });

            // Normalize target to step boundary + clamp to legal range.
            var target = body.target_bonus;
            if (target < 0) target = 0;
            if (target > BonusMax) target = BonusMax;
            target = (target / BonusStep) * BonusStep;

            // Look up current bonus to compute cost. The DB transaction in
            // PurchaseObjectLimitBonus re-reads under FOR UPDATE so this is just for
            // pricing; the authoritative re-check happens inside the transaction.
            DbObjectLimitBonusPurchase result;
            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.Get(lotId);
                if (lot == null)
                    return NotFound(new { error = "lot_not_found" });

                int fromStep = lot.object_limit_bonus / BonusStep;
                int toStep = target / BonusStep;
                int cost = BonusPrices[toStep] - BonusPrices[fromStep];
                if (cost < 0) cost = 0;

                result = da.Lots.PurchaseObjectLimitBonus(lotId, body.avatar_id, target, cost);
            }

            switch (result.Status)
            {
                case ObjectLimitBonusPurchaseStatus.Success:
                    break;
                case ObjectLimitBonusPurchaseStatus.LotNotFound:
                    return NotFound(new { error = "lot_not_found" });
                case ObjectLimitBonusPurchaseStatus.AvatarNotFound:
                    return NotFound(new { error = "avatar_not_found" });
                case ObjectLimitBonusPurchaseStatus.NotLotOwner:
                    return StatusCode((int)HttpStatusCode.Forbidden, new { error = "not_lot_owner" });
                case ObjectLimitBonusPurchaseStatus.NotAnUpgrade:
                    return BadRequest(new { error = "not_an_upgrade" });
                case ObjectLimitBonusPurchaseStatus.InsufficientFunds:
                    return BadRequest(new { error = "insufficient_funds" });
                case ObjectLimitBonusPurchaseStatus.DbError:
                default:
                    return StatusCode(500, new { error = "db_error" });
            }

            // Live update: tell every lot server the new value. Each server checks if
            // it's hosting this LotId and applies via VMNetSetObjectLimitBonusCmd; all
            // others ignore. If the lot isn't currently hosted anywhere, the DB write
            // we just did is sufficient — LotContainer reads object_limit_bonus on host.
            int notified = 0;
            if (api.HostPool != null)
            {
                var packet = new SetLotObjectLimitBonus
                {
                    LotId = (uint)lotId,
                    TargetBonus = result.NewBonus,
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
                lot_id = lotId,
                avatar_id = body.avatar_id,
                new_bonus = result.NewBonus,
                cost_charged = result.CostCharged,
                new_budget = result.NewBudget,
                lot_servers_notified = notified,
            });
        }
    }
}