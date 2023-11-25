using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.DataService.Model;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.Controllers.Panels
{
    public class LotAdmitController
    {
        private Network.Network Network;
        private IClientDataService DataService;
        private UIAdmitBanPanel View;
        private Binding<Lot> Binding;
        private bool ShowBans;

        public LotAdmitController(UIAdmitBanPanel view, IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
            this.View = view;
            this.Binding = new Binding<Lot>().WithBinding(View, "Mode", "Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode")
                .WithMultiBinding(x => { RefreshResults(); }, "Lot_LotAdmitInfo.LotAdmitInfo_AdmitList", "Lot_LotAdmitInfo.LotAdmitInfo_BanList");
            Init();
        }

        private void Init()
        {
            DataService.Request(MaskedStruct.AdmitInfo_Lot, View.LotID).ContinueWith(x =>
            {
                Binding.Value = (Lot)x.Result;
            });
        }

        public void SetBanMode(bool mode)
        {
            ShowBans = mode;
            RefreshResults();
        }

        public void RefreshResults()
        {
            var list = new List<Avatar>();
            if (Binding.Value != null && Binding.Value.Lot_LotAdmitInfo?.LotAdmitInfo_AdmitList != null)
            {
                List<uint> source;
                if (ShowBans) source = Binding.Value.Lot_LotAdmitInfo.LotAdmitInfo_BanList.ToList();
                else source = Binding.Value.Lot_LotAdmitInfo.LotAdmitInfo_AdmitList.ToList();
                list = DataService.EnrichList<Avatar, uint, Avatar>(source, x => x, (avatarid, avatar) =>
                {
                    return avatar;
                });
            }

            View.SetResults(list);
        }

        public void UpdateMode(byte mode)
        {
            if (Binding.Value != null && Binding.Value.Lot_LotAdmitInfo != null)
            {
                Binding.Value.Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode = mode;
                DataService.Sync(Binding.Value, new string[] { "Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode" });
            }
            RefreshResults();
        }

        public void Dispose()
        {
        }
    }
}
