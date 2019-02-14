using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    public class BulletinDialogController
    {
        private Network.Network Network;
        private IClientDataService DataService;
        private UIBulletinDialog View;

        private BulletinActionController Actions;
        private uint NhoodID;
        //private Binding<Avatar> Binding;

        public BulletinDialogController(UIBulletinDialog view, IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
            this.View = view;
            //this.Binding = new Binding<Avatar>().WithMultiBinding(x => { RefreshResults(); }, "Avatar_BookmarksVec");

            this.Actions = UIScreen.Current.FindController<CoreGameScreenController>()?.BulletinProtocol;

            //Init();
        }

        public void LoadNhood(uint nhoodID)
        {
            NhoodID = nhoodID;
            DataService.Request(MaskedStruct.NeighPage_Info, nhoodID).ContinueWith(x =>
            {
                DataService.Request(MaskedStruct.NeighPage_Mayor, nhoodID);
                View.CurrentNeigh.Value = (Neighborhood)x.Result;
            });

            Actions?.GetMessages(nhoodID, (response) =>
            {
                if (Actions.BulletinBoard != null)
                {
                    foreach (var item in Actions.BulletinBoard)
                    {
                        //system posts can have CST replacement, identical to mail.
                        if (item.Type == Files.Formats.tsodata.BulletinType.System)
                        {
                            var cst = MessagingController.CSTReplace(item.SenderName, item.Subject, item.Body);
                            item.SenderName = cst.Item1;
                            item.Subject = cst.Item2;
                            item.Body = cst.Item3;
                        }
                    }
                    View.Board.InitBulletinItems(Actions.BulletinBoard);
                }
            });
        }

        public void TransitionToPost()
        {
            //check
            if (Actions == null)
            {
                View.SelectedPost(null);
                return;
            }
            View.Board.Lock();
            Actions.BeginPost(NhoodID, (response) =>
            {
                if (response == BulletinResponseType.SUCCESS)
                {
                    View.SelectedPost(null);
                }
                else
                {
                    View.Board.Unlock();
                }
            });
        }

        public void MakePost(string title, string message, uint lotID, bool system)
        {
            View.Post.Lock();
            Actions?.MakePost(NhoodID, title, message, lotID, system, (result) =>
            {
                if (result == BulletinResponseType.SUCCESS)
                {
                    var posts = Actions.BulletinBoard;
                    if (posts != null && Actions.CreatedMessage != null)
                    {
                        var newPosts = posts.ToList();
                        newPosts.Insert(0, Actions.CreatedMessage);
                        posts = newPosts.ToArray();
                        Actions.BulletinBoard = posts;
                        View.Board.InitBulletinItems(posts);
                    }
                    UIAlert.Alert("", GameFacade.Strings.GetString("f121", "3", new string[] { View.Post.IsMayor ? "1" : "3" }), true);
                    View.Return();
                }
                else
                {
                    View.Post.Unlock();
                }
            });
        }

        public void Delete(uint postID)
        {
            View.Post.Lock();
            Actions?.DeleteMessage(postID, (result) =>
            {
                if (result == BulletinResponseType.SUCCESS)
                {
                    var item = View.Post.ActiveItem;
                    if (Actions.BulletinBoard != null && item != null)
                    {
                        Actions.BulletinBoard = Actions.BulletinBoard.Where(x => x != item).ToArray();
                        View.Board.InitBulletinItems(Actions.BulletinBoard);
                    }
                    View.Return();
                }
                else
                {
                    View.Post.Unlock();
                }
            });
        }

        public void Promote(uint postID)
        {
            View.Post.Lock();
            Actions?.PromoteMessage(postID, (result) =>
            {
                if (result == BulletinResponseType.SUCCESS)
                {
                    var item = View.Post.ActiveItem;
                    if (item != null)
                    {
                        item.Flags |= Files.Formats.tsodata.BulletinFlags.PromotedByMayor;
                        item.Type = Files.Formats.tsodata.BulletinType.Mayor;
                        View.Board.InitBulletinItems(Actions.BulletinBoard);
                    }
                    View.Return();
                }
                else
                {
                    View.Post.Unlock();
                }
            });
        }

        public void Dispose()
        {
        }
    }
}
