using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class BulletinActionController : IDisposable
    {
        private UIElement BlockingDialog;
        GenericActionRegulator<BulletinRequest, BulletinResponse> ConnectionReg;
        private List<Callback<BulletinResponseType>> Callbacks = new List<Callback<BulletinResponseType>>();
        private bool Blocked = false;

        public BulletinItem[] BulletinBoard;
        public BulletinItem CreatedMessage;

        public BulletinActionController(GenericActionRegulator<BulletinRequest, BulletinResponse> regulator)
        {
            regulator.OnError += Regulator_OnError;
            regulator.OnTransition += Regulator_OnTransition;

            ConnectionReg = regulator;
        }

        public void Dispose()
        {
            ConnectionReg.OnError -= Regulator_OnError;
            ConnectionReg.OnTransition -= Regulator_OnTransition;
        }

        public void BeginPost(uint nhoodID, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = BulletinRequestType.CAN_POST_MESSAGE,
                Value = 1,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void BeginSystemPost(uint nhoodID, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = BulletinRequestType.CAN_POST_SYSTEM_MESSAGE,
                Value = 1,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void MakePost(uint nhoodID, string title, string message, uint lotID, bool system, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = system ? BulletinRequestType.POST_SYSTEM_MESSAGE : BulletinRequestType.POST_MESSAGE,
                Title = title,
                Message = message,
                LotID = lotID,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void DeleteMessage(uint messageID, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = BulletinRequestType.DELETE_MESSAGE,
                Value = messageID,
            });
            Callbacks.Add(callback);
        }

        public void PromoteMessage(uint messageID, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = BulletinRequestType.PROMOTE_MESSAGE,
                Value = messageID,
            });
            Callbacks.Add(callback);
        }

        public void GetMessages(uint nhoodID, Callback<BulletinResponseType> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new BulletinRequest()
            {
                Type = BulletinRequestType.GET_MESSAGES,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        private void ResolveCallbacks(BulletinResponseType code)
        {
            GameThread.InUpdate(() =>
            {
                foreach (var cb in Callbacks)
                {
                    cb.Invoke(code);
                }
                Callbacks.Clear();
                Blocked = false;
            });
        }

        private void Regulator_OnError(object data)
        {
            var errorTitle = GameFacade.Strings.GetString("211", "45");
            var errorBody = GameFacade.Strings.GetString("211", "45");

            if (data is BulletinResponse)
            {
                var response = data as BulletinResponse;
                if (response.Type == BulletinResponseType.SEND_FAIL_GAMEPLAY_BAN)
                {
                    //print message and end date
                    errorTitle = GameFacade.Strings.GetString("f117", "1");
                    errorBody = GameFacade.Strings.GetString("f117", "18",
                        new string[] {
                            ClientEpoch.DHMRemaining(response.BanEndDate),
                            response.Message
                        });
                }
                else if (response.Type == BulletinResponseType.FAIL_UNKNOWN)
                {
                    errorTitle = GameFacade.Strings.GetString("f121", "1");
                    errorBody = GameFacade.Strings.GetString("f121", "2");
                    if (response.Message != "")
                    {
                        errorBody += "\n\n" + response.Message;
                    }
                }
                else
                {
                    errorTitle = GameFacade.Strings.GetString("f121", "1");
                    errorBody = GameFacade.Strings.GetString("f121", ((int)response.Type + 1).ToString(), new string[] { "3", "1" });
                }
                ResolveCallbacks(response.Type);
            }
            else
            {
                ResolveCallbacks(BulletinResponseType.FAIL_UNKNOWN);
            }

            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = errorTitle,
                Message = errorBody,
                Buttons = UIAlertButton.Ok(x => {
                    UIScreen.RemoveDialog(BlockingDialog);
                    UIScreen.RemoveDialog(alert);
                }),
                AllowEmojis = true
            }, true);
        }

        private void Regulator_OnTransition(string state, object data)
        {
            var progress = 0;

            GameThread.InUpdate(() =>
            {
                switch (state)
                {
                    case "Idle":
                        if (Callbacks.Count > 0)
                        {
                            ResolveCallbacks(BulletinResponseType.CANCEL);
                        }
                        if (BlockingDialog != null)
                        {
                            UIScreen.RemoveDialog(BlockingDialog);
                            BlockingDialog = null;
                        }
                        break;

                    case "ActionInput":
                        //show blocking dialog for this action
                        //not used for bulletin right now
                        var req = ConnectionReg.CurrentRequest;
                        switch (req.Type)
                        {
                            case BulletinRequestType.CAN_POST_MESSAGE:
                            case BulletinRequestType.CAN_POST_SYSTEM_MESSAGE:
                                break;
                            default:
                                //something went terribly wrong - we don't have any dialog to handle this.
                                ConnectionReg.AsyncReset();
                                break;
                        }
                        break;
                    case "ActionSuccess":
                        var packet = (BulletinResponse)data;
                        if (packet.Type == BulletinResponseType.SEND_SUCCESS)
                        {
                            CreatedMessage = packet.Messages[0];
                        }
                        else if (packet.Type == BulletinResponseType.MESSAGES)
                        {
                            BulletinBoard = packet.Messages;
                        }
                        ResolveCallbacks(BulletinResponseType.SUCCESS);
                        break;
                }
            });
        }
    }
}
