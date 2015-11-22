using FSO.Server.Database.DA;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    public class LotHost
    {
        private Dictionary<uint, LotContainer> Lots = new Dictionary<uint, LotContainer>();
        private LotServerConfiguration Config;
        private IDAFactory DAFactory;
        private IKernel Kernel;

        public LotHost(LotServerConfiguration config, IDAFactory da, IKernel kernel)
        {
            this.Config = config;
            this.DAFactory = da;
            this.Kernel = kernel;
        }

        public LotContainer TryHost(uint id)
        {
            lock (Lots)
            {
                if(Lots.Values.Count >= Config.Max_Lots)
                {
                    //No room
                    return null;
                }

                if (Lots.ContainsKey(id))
                {
                    return null;
                }

                var ctnr = Kernel.Get<LotContainer>();
                Lots.Add(id, ctnr);
                return ctnr;
            }
        }

        public bool TryAcceptClaim(uint lotId, uint claimId, string previousOwner)
        {
            using (var da = DAFactory.Get())
            {
                var didClaim = da.LotClaims.Claim(claimId, previousOwner, Config.Call_Sign);
                if (!didClaim)
                {
                    Lots.Remove(lotId);
                    return false;
                }
                else
                {
                    var claim = da.LotClaims.Get(claimId);
                    if(claim == null)
                    {
                        Lots.Remove(lotId);
                        return false;
                    }

                    var lot = da.Lots.Get(claim.lot_id);
                    if(lot == null)
                    {
                        Lots.Remove(lotId);
                        return false;
                    }

                    Lots[lotId].Bootstrap(new LotContext {
                        DbId = lot.lot_id,
                        Id = lot.location,
                        ClaimId = claimId,
                        ShardId = lot.shard_id 
                    });
                    return true;
                }
            }
        }

    }
}
