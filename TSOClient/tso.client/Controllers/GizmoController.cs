using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using System.Collections.Immutable;

namespace FSO.Client.Controllers
{
    public class GizmoController : IDisposable
    {
        private UIGizmo Gizmo;
        private Network.Network Network;
        private IClientDataService DataService;

        public ImmutableList<uint> FilterList
        {
            set
            {
                HandleFilterList(value);
            }
        }

        public GizmoController(UIGizmo view, Network.Network network, IClientDataService dataService)
        {
            this.Gizmo = view;
            this.Network = network;
            this.DataService = dataService;
            this.Gizmo.CurrentAvatar
                .WithBinding(this, "FilterList", "Avatar_Top100ListFilter.Top100ListFilter_ResultsVec");

            Initialize();
        }

        private void Initialize()
        {
            DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
            {
                if (!x.IsFaulted){
                    Gizmo.CurrentAvatar.Value = x.Result;
                    FSO.UI.Model.DiscordRpcEngine.SendFSOPresence(x.Result.Avatar_Name, null, 0, 0, 0, 0, null, x.Result.Avatar_PrivacyMode > 0);

                    if (Network.Mode == Regulators.CityConnectionMode.ARCHIVE)
                    {
                        RequestFilter(LotCategory.archive_welcome);
                    }
                    else if (x.Result.Avatar_Age < 14)
                    {
                        RequestFilter(LotCategory.welcome);
                    }
                }
            });
        }

        private void HandleFilterList(ImmutableList<uint> lots)
        {
            if (Gizmo.CurrentAvatar.Value != null && Gizmo.CurrentAvatar.Value.Avatar_Top100ListFilter.Top100ListFilter_Top100ListID == (uint)LotCategory.archive_welcome && lots.Count == 1)
            {
                // If the player isn't currently on a lot, and this archive_welcome lot has a hint that hasn't been seen yet, then make them automatically join it.
                uint targetLot = lots[0];

                var controller = UIScreen.Current.FindController<CoreGameScreenController>();

                if (controller != null && !controller.IsLotSelected())
                {
                    var hints = FSOFacade.Hints;
                    string trigger = $"lot:{GameFacade.CurrentCityName}:{targetLot}";

                    if (!hints.IsHintTriggered(trigger))
                    {
                        controller.JoinLot(targetLot);
                    }
                }
            }
        }

        public void Dispose()
        {
            try {
                Gizmo.CurrentAvatar.Value = null;
            }catch(Exception ex){
            }
        }

        public void RequestFilter(LotCategory cat)
        {
            if (Gizmo.CurrentAvatar != null && Gizmo.CurrentAvatar.Value != null)
            {
                Gizmo.CurrentAvatar.Value.Avatar_Top100ListFilter.Top100ListFilter_Top100ListID = (uint)cat;
                DataService.Sync(Gizmo.CurrentAvatar.Value, new string[] { "Avatar_Top100ListFilter.Top100ListFilter_Top100ListID" });
            }
        }

        public void ClearFilter()
        {
            Gizmo.FilterList = System.Collections.Immutable.ImmutableList<uint>.Empty;
        }
    }
}
