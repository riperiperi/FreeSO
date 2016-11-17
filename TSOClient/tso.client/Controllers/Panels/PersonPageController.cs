using FSO.Client.Network;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FSO.Client.Controllers
{
    public class PersonPageController : IAriesMessageSubscriber, IDisposable
    {
        private UIPersonPage View;
        private IClientDataService DataService;
        private Network.Network Network;
        private uint AvatarId;

        private ITopicSubscription Topic;

        private GameThreadTimeout MyRelPollTimeout;
        private bool MyRelDirty = true;

        public PersonPageController(UIPersonPage view, IClientDataService dataService, Network.Network network)
        {
            this.View = view;
            this.DataService = dataService;
            this.Network = network;
            Topic = dataService.CreateTopicSubscription();

            Network.CityClient.AddSubscriber(this);
        }

        ~PersonPageController(){
            Topic.Dispose();
            MyRelPollTimeout?.Clear();
        }

        public void Close()
        {
            View.TrySaveDescription();
            View.Visible = false;
            ChangeTopic();
        }

        public void Show(uint avatarId){
            View.TrySaveDescription();
            AvatarId = avatarId;
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                View.CurrentAvatar.Value = x.Result;
            });

            if (View.MyAvatar.Value == null)
            {
                DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
                {
                    View.MyAvatar.Value = x.Result;
                    if (x.Result != null && x.Result.Avatar_LotGridXY != 0 && (View.MyLot.Value == null || View.MyLot.Value.Lot_Location_Packed != x.Result.Avatar_LotGridXY))
                    {
                        DataService.Request(MaskedStruct.AdmitInfo_Lot, x.Result.Avatar_LotGridXY).ContinueWith(y =>
                        {
                            View.MyLot.Value = (Lot)y.Result;
                        });
                    }
                });
            } else
            {
                if (View.MyAvatar.Value != null && View.MyAvatar.Value.Avatar_LotGridXY != 0 && (View.MyLot.Value == null || View.MyLot.Value.Lot_Location_Packed != View.MyAvatar.Value.Avatar_LotGridXY))
                {
                    DataService.Request(MaskedStruct.AdmitInfo_Lot, View.MyAvatar.Value.Avatar_LotGridXY).ContinueWith(y =>
                    {
                        View.MyLot.Value = (Lot)y.Result;
                    });
                }
            }

            View.CurrentTab = UIPersonPageTab.Description;
            View.SetOpen(false);
            View.Parent.Add(View);
            View.Visible = true;
            ChangeTopic();
        }

        public void RemoveBookmark(Avatar target, Bookmark bookmark)
        {
            DataService.RemoveFromArray(target, "Avatar_BookmarksVec", bookmark);
            DataService.Request(MaskedStruct.MyAvatar, Network.MyCharacter);
        }

        public void AddBookmark(Avatar target, Bookmark bookmark)
        {
            DataService.AddToArray(target, "Avatar_BookmarksVec", bookmark);
            DataService.Request(MaskedStruct.MyAvatar, Network.MyCharacter);
        }

        public void RemoveAdmitBan(Lot target, uint target_avatar, bool ban)
        {
            var mode = (ban) ? "Ban" : "Admit";
            DataService.RemoveFromArray(target, "Lot_LotAdmitInfo.LotAdmitInfo_"+mode+"List", target_avatar);
            DataService.Request(MaskedStruct.AdmitInfo_Lot, target.Id);
        }

        public void AddAdmitBan(Lot target, uint target_avatar, bool ban)
        {
            var mode = (ban) ? "Ban" : "Admit";
            DataService.AddToArray(target, "Lot_LotAdmitInfo.LotAdmitInfo_" + mode + "List", target_avatar);
            DataService.Request(MaskedStruct.AdmitInfo_Lot, target.Id);
        }

        public void FindAvatarLocation()
        {
            Network.CityClient.Write(new FindAvatarRequest()
            {
                AvatarId = AvatarId
            });
        }

        public void SaveDescription(Avatar target)
        {
            DataService.Sync(target, new string[] { "Avatar_Description" });
        }

        public void SaveValue(Avatar target, string name)
        {
            DataService.Sync(target, new string[] { name });
        }

        public void ForceRefreshData(UIPersonPageTab tab){
            Topic.Poll();
        }

        public void ChangeTopic()
        {
            List<ITopic> topics = new List<ITopic>();
            if (View.Visible && AvatarId != 0)
            {
                topics.Add(Topics.For(MaskedStruct.SimPage_Main, AvatarId));
                switch (View.CurrentTab)
                {
                    case UIPersonPageTab.Description:
                        topics.Add(Topics.For(MaskedStruct.SimPage_DescriptionPanel, AvatarId));
                        break;
                    case UIPersonPageTab.Accomplishments:
                        topics.Add(Topics.For(MaskedStruct.SimPage_SkillsPanel, AvatarId));
                        topics.Add(Topics.For(MaskedStruct.SimPage_JobsPanel, AvatarId));
                        break;
                    case UIPersonPageTab.Relationships:
                        //if we're due a relationship poll on our own avatar, perform it.
                        if (MyRelDirty)
                        {
                            MyRelDirty = false;
                            DataService.Request(MaskedStruct.FriendshipWeb_Avatar, Network.MyCharacter);
                            MyRelPollTimeout = GameThread.SetTimeout(() => { MyRelDirty = true; }, 60000);
                        }
                        break;
                }
            }
            Topic.Set(topics);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is FindAvatarResponse)
            {
                var loc = (FindAvatarResponse)message;
                GameThread.InUpdate(() =>
                {
                    switch (loc.Status)
                    {
                        case FindAvatarResponseStatus.FOUND:
                            View.FindController<CoreGameScreenController>()?.ShowLotPage(loc.LotId & 0x3FFFFFFF); //ignore transient part
                            break;
                        default:
                            if (loc.Status == FindAvatarResponseStatus.PRIVACY_ENABLED) loc.Status = FindAvatarResponseStatus.NOT_ON_LOT;
                            UIAlert alert = null;
                            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                            {
                                Title = "",
                                Message = GameFacade.Strings.GetString("189", (49+(int)loc.Status).ToString()),
                                Buttons = UIAlertButton.Ok((btn) => UIScreen.RemoveDialog(alert)),
                                Alignment = TextAlignment.Left
                            }, true);
                            break;
                    }
                });
            }
        }

        public void Dispose()
        {
            Network.CityClient.RemoveSubscriber(this);
        }
    }
}
