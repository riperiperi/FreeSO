using FSO.Client.UI;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
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

namespace FSO.Client.Controllers
{
    public class RoommateRequestController : IAriesMessageSubscriber, IDisposable
    {
        private CoreGameScreenController Game;
        private Network.Network Network;
        private IClientDataService DataService;
        public RoommateRequestController(CoreGameScreenController game, Network.Network network, IClientDataService dataService)
        {
            this.Game = game;
            this.Network = network;
            this.DataService = dataService;

            this.Network.CityClient.AddSubscriber(this);

            Network.CityClient.Write(new ChangeRoommateRequest
            {
                Type = ChangeRoommateType.POLL, //we're ready to recieve any pending roommate requests
            });
        }

        public void Dispose()
        {
            this.Network.CityClient.RemoveSubscriber(this);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is ChangeRoommateRequest)
            {
                var req = (ChangeRoommateRequest)message;
                if (req.Type == ChangeRoommateType.INVITE)
                {
                    DataService.Request(MaskedStruct.SimPage_Main, req.AvatarId).ContinueWith(x =>
                    {
                        GameThread.InUpdate(() =>
                        {
                            if (((Avatar)x.Result)?.Avatar_Name == null) return;
                            var name = ((Avatar)x.Result).Avatar_Name;

                            UIAlert alert = null;
                            alert = new UIAlert(new UIAlertOptions()
                            {
                                Title = GameFacade.Strings.GetString("208", "10"),
                                Message = GameFacade.Strings.GetString("208", "11", new string[] { name }),
                                Buttons = new UIAlertButton[]
                                {
                                new UIAlertButton(UIAlertButtonType.Yes, btn => {
                                    Network.CityClient.Write(new ChangeRoommateRequest
                                    {
                                        Type = ChangeRoommateType.ACCEPT,
                                        AvatarId = req.AvatarId,
                                        LotLocation = req.LotLocation
                                    });
                                    UIScreen.RemoveDialog(alert);
                                }),
                                new UIAlertButton(UIAlertButtonType.No, btn => {
                                    Network.CityClient.Write(new ChangeRoommateRequest
                                    {
                                        Type = ChangeRoommateType.DECLINE,
                                        AvatarId = req.AvatarId,
                                        LotLocation = req.LotLocation
                                    });
                                    UIScreen.RemoveDialog(alert);
                                })
                                }
                            });
                            UIScreen.GlobalShowDialog(alert, true);
                        });
                    });
                }
            }
            else if (message is ChangeRoommateResponse)
            {
                string title;
                string msg;
                var resp = (ChangeRoommateResponse)message;
                switch (resp.Type)
                {
                    case ChangeRoommateResponseStatus.ACCEPT_SUCCESS:
                        title = GameFacade.Strings.GetString("208", "50");
                        msg = GameFacade.Strings.GetString("208", "53");
                        break;
                    case ChangeRoommateResponseStatus.KICK_SUCCESS:
                        title = GameFacade.Strings.GetString("208", "70");
                        msg = GameFacade.Strings.GetString("208", "73");
                        break;  
                    case ChangeRoommateResponseStatus.SELFKICK_SUCCESS:
                        title = GameFacade.Strings.GetString("208", "130");
                        msg = GameFacade.Strings.GetString("208", "133");
                        break;
                    case ChangeRoommateResponseStatus.DECLINE_SUCCESS:
                    case ChangeRoommateResponseStatus.INVITE_SUCCESS:
                        return;
                    case ChangeRoommateResponseStatus.LOT_MUST_BE_CLOSED:
                    case ChangeRoommateResponseStatus.YOU_ARE_NOT_ROOMMATE:
                        //unable to kickout
                        title = GameFacade.Strings.GetString("208", "80");
                        msg = GameFacade.Strings.GetString("208", "83");
                        break;
                    case ChangeRoommateResponseStatus.ROOMIE_ELSEWHERE:
                    case ChangeRoommateResponseStatus.OTHER_INVITE_PENDING:
                        title = GameFacade.Strings.GetString("208", "40");
                        msg = GameFacade.Strings.GetString("208", "43");
                        break;
                    case ChangeRoommateResponseStatus.ROOMMATE_LEFT:
                        DataService.Request(MaskedStruct.SimPage_Main, resp.Extra).ContinueWith(x =>
                        {
                            GameThread.InUpdate(() =>
                            {
                                if (((Avatar)x.Result)?.Avatar_Name == null) return;
                                var name = ((Avatar)x.Result).Avatar_Name;
                                UIScreen.GlobalShowDialog(new UIAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("208", "100"),
                                    Message = GameFacade.Strings.GetString("208", "101", new string[] { name }),
                                }), true);

                            });
                        });
                        return;
                    case ChangeRoommateResponseStatus.GOT_KICKED:
                        title = GameFacade.Strings.GetString("208", "90");
                        msg = GameFacade.Strings.GetString("208", "93");
                        break;
                    case ChangeRoommateResponseStatus.UNKNOWN:
                    case ChangeRoommateResponseStatus.TOO_MANY_ROOMMATES:
                    case ChangeRoommateResponseStatus.YOU_ARE_NOT_OWNER:
                    case ChangeRoommateResponseStatus.NO_INVITE_PENDING:
                    case ChangeRoommateResponseStatus.LOT_DOESNT_EXIST:
                    default:
                        //invitation failed
                        title = GameFacade.Strings.GetString("208", "40");
                        msg = GameFacade.Strings.GetString("208", "43");
                        break;

                }
                UIScreen.GlobalShowDialog(new UIAlert(new UIAlertOptions()
                {
                    Title = title,
                    Message = msg,
                }), true);

                //got kicked out
                //title = GameFacade.Strings.GetString("208", "94");
                //msg = GameFacade.Strings.GetString("208", "93");
            }
        }
    }
}
