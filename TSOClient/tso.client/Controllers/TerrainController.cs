using FSO.Client.Rendering.City;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class TerrainController
    {
        private CoreGameScreenController Parent;
        private Terrain View;
        private IClientDataService DataService;
        private IShardRealestateDomain Realestate;

        private Binding<Lot> CurrentHoverLot;

        public TerrainController(CoreGameScreenController parent, IClientDataService ds, Network.Network network, IRealestateDomain domain){
            this.Parent = parent;
            this.DataService = ds;

            Realestate = domain.GetByShard(network.MyShard.Id);

            CurrentHoverLot = new Binding<Lot>()
                .WithBinding(this, "Lot_Price", "Lot_Price");
        }

        private uint _LotPrice;
        public uint Lot_Price
        {
            get { return _LotPrice; }
            set
            {
                _LotPrice = value;
                System.Diagnostics.Debug.WriteLine(_LotPrice);
            }
        }

        public void Init(Terrain terrain){
            View = terrain;
        }

        public bool IsPurchasable(int x, int y)
        {
            return Realestate.IsPurchasable((ushort)x, (ushort)y);
        }

        public void HoverTile(int x, int y)
        {
            if (Realestate.IsPurchasable((ushort)x, (ushort)y))
            {
                var id = MapCoordinates.Pack((ushort)x, (ushort)y);
                DataService.Get<Lot>(id).ContinueWith(lot =>
                {
                    CurrentHoverLot.Value = lot.Result;

                    //Not loaded yet
                    if(lot.Result.Lot_Price == 0){
                        DataService.Request(Server.DataService.Model.MaskedStruct.MapView_RollOverInfo_Lot_Price, id);
                    }
                });
            }
            else
            {
                CurrentHoverLot.Value = null;
            }
        }

        public void ClickLot(int x, int y)
        {
            
            /**
                UIAlertOptions AlertOptions = new UIAlertOptions();
                AlertOptions.Title = GameFacade.Strings.GetString("246", "1");
                AlertOptions.Message = GameFacade.Strings.GetString("215", "23", new string[] 
                { m_LotCost.ToString(), CurrentUIScr.ucp.MoneyText.Caption });
                AlertOptions.Buttons = new UIAlertButton[] {
                    new UIAlertButton(UIAlertButtonType.Yes, new ButtonClickDelegate(BuyPropertyAlert_OnButtonClick)),
                    new UIAlertButton(UIAlertButtonType.No) };

                m_BuyPropertyAlert = UIScreen.GlobalShowAlert(AlertOptions, true);**/
        }
    }
}
