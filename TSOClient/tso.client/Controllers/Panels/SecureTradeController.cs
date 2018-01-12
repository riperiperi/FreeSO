using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    /// <summary>
    /// Used to obtain our lot's name.
    /// </summary>
    public class SecureTradeController
    {
        private Network.Network Network;
        private IClientDataService DataService;

        public SecureTradeController(IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
        }

        /// <summary>
        /// Finds a lot owned by us, then returns its name, along with if we are its owner.
        /// </summary>
        /// <param name="callback">A function to take the name and owner status found. Name is null if a lot was not found.</param>
        public void GetOurLotsName(Action<string, bool> callback)
        {
            DataService.Request(MaskedStruct.SimPage_Main, Network.MyCharacter)
                .ContinueWith(x =>
                {
                    var avatar = x.Result as Avatar;
                    if (avatar == null) return;

                    var lotLoc = avatar.Avatar_LotGridXY;
                    if (lotLoc == 0) GameThread.NextUpdate(state => callback(null, false));
                    else
                    {
                        DataService.Request(MaskedStruct.PropertyPage_LotInfo, lotLoc).ContinueWith(y =>
                        {
                            var lot = y.Result as Lot;
                            if (lot == null) GameThread.NextUpdate(state => callback(null, false));
                            else {
                                GameThread.NextUpdate(state => {
                                    callback(lot.Lot_Name, lot.Lot_LeaderID == Network.MyCharacter);
                                });
                            }
                        });
                    }
                });
        }
    }
}
