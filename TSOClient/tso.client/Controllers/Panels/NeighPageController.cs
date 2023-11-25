using FSO.Client.Network;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Client.Utils;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.DataService.Model;
using Ninject;
using System.Collections.Generic;
using FSO.Client.UI.Controls;

namespace FSO.Client.Controllers.Panels
{
    class NeighPageController
    {
        private UINeighPage View;
        private IClientDataService DataService;
        private ITopicSubscription Topic;
        private uint NeighId;

        public NeighPageController(UINeighPage view, IClientDataService dataService)
        {
            this.View = view;
            this.DataService = dataService;
            this.Topic = dataService.CreateTopicSubscription();

            ControllerUtils.BindController<RatingSummaryController>(this.View.MayorRatingBox1);
            ControllerUtils.BindController<RatingSummaryController>(this.View.MayorRatingBox2);
        }

        ~NeighPageController()
        {
            Topic.Dispose();
        }

        public void GetMayorInfo(uint avatarID)
        {
            DataService.Request(MaskedStruct.MayorInfo_Avatar, avatarID).ContinueWith(x =>
            {
                View.CurrentMayor.Value = (Avatar)x.Result;
            });
        }


        public void GetTownHallInfo(uint lotID)
        {
            DataService.Request(MaskedStruct.Bookmark_Lot, lotID).ContinueWith(x =>
            {
                View.CurrentTownHall.Value = (Lot)x.Result;
            });
        }

        public void ModSetMayor()
        {
            UIAlert.Prompt("", "Enter an avatar name to set as the mayor of this Neighborhood.", true, (search) =>
            {
                if (search == null) return;

                var cont = FSOFacade.Kernel.Get<GenericSearchController>();
                cont.Search(search, true, avas =>
                {
                    if (avas == null || avas.Count == 0)
                    {
                        UIAlert.Alert("", "Could not find that avatar.", true);
                        return;
                    }
                    var target = avas[0];
                    UIAlert.YesNo("", $"This action will replace the current mayor with {target.Result.Name}. Is this OK?", true,
                        (response) =>
                        {
                            if (response) {
                                View.FindController<CoreGameScreenController>().NeighborhoodProtocol.SetMayor(NeighId, target.Result.EntityId,
                                    (success) =>
                                    {
                                        if (success == Server.Protocol.Electron.Packets.NhoodResponseCode.SUCCESS)
                                        {
                                            UIAlert.Alert("", $"Successfully set the Mayor to {target.Result.Name}", true);
                                            ChangeTopic();
                                        }
                                    });
                            }
                        });
                });
            });
        }

        public void Close()
        {
            View.TrySaveDescription();
            View.Visible = false;
            ChangeTopic();
            var gizmo = (UI.Framework.UIScreen.Current as UI.Screens.CoreGameScreen)?.gizmo;
            if (gizmo != null)
            {
                gizmo.FilterList = System.Collections.Immutable.ImmutableList<uint>.Empty;
            }
        }

        public void Show(uint lotId)
        {
            View.TrySaveDescription();
            NeighId = lotId;
            DataService.Get<Neighborhood>(lotId).ContinueWith(x =>
            {
                View.CurrentNeigh.Value = x.Result;
                DataService.Request(MaskedStruct.NeighPage_Info, lotId);
                DataService.Request(MaskedStruct.NeighPage_TopLots, lotId);
            });
            View.Parent.Add(View);
            View.HasShownFilters = false;
            View.DescriptionChanged = false;
            View.Visible = true;
            //View.AsyncAPIThumb(lotId);
            var gizmo = (UI.Framework.UIScreen.Current as UI.Screens.CoreGameScreen)?.gizmo;
            if (gizmo != null)
            {
                gizmo.FilterList = System.Collections.Immutable.ImmutableList<uint>.Empty;
            }
            ChangeTopic();
            FSOFacade.Hints.TriggerHint("ui:neigh_page");
        }

        /*
        public void SaveDescription(Lot target)
        {
            DataService.Sync(target, new string[] { "Lot_Description" });
        }

        public void SaveName(Lot target)
        {
            DataService.Sync(target, new string[] { "Lot_Name" });
        }
        */

        public void ChangeTopic()
        {
            List<ITopic> topics = new List<ITopic>();
            if (View.Visible && NeighId != 0)
            {
                topics.Add(Topics.For(MaskedStruct.NeighPage_Info, NeighId));
                switch (View.CurrentTab)
                {
                    case UINeighPageTab.Description:
                        topics.Add(Topics.For(MaskedStruct.NeighPage_Description, NeighId));
                        break;
                    case UINeighPageTab.Lots:
                        topics.Add(Topics.For(MaskedStruct.NeighPage_TopLots, NeighId));
                        break;
                    case UINeighPageTab.Mayor:
                        topics.Add(Topics.For(MaskedStruct.NeighPage_Mayor, NeighId));
                        break;
                    case UINeighPageTab.People:
                        topics.Add(Topics.For(MaskedStruct.NeighPage_TopAvatars, NeighId));
                        break;
                }
            }
            Topic.Set(topics);
        }

        public void SaveDescription(Neighborhood target)
        {
            DataService.Sync(target, new string[] { "Neighborhood_Description" });
        }

        public void SaveName(Neighborhood target)
        {
            DataService.Sync(target, new string[] { "Neighborhood_Name" });
        }
    }
}
