using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class NeighborhoodActionController : IDisposable
    {
        private UIElement BlockingDialog;
        GenericActionRegulator<NhoodRequest, NhoodResponse> ConnectionReg;
        private List<Callback<NhoodResponseCode>> Callbacks = new List<Callback<NhoodResponseCode>>();
        public string LastMessage = "";
        private bool Blocked = false;

        public NeighborhoodActionController(GenericActionRegulator<NhoodRequest, NhoodResponse> regulator)
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

        public void BeginRating(uint nhoodID, uint avatarID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.CAN_RATE,
                TargetAvatar = avatarID,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void BanUser(uint avatarID, uint untilDate, string message, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.NHOOD_GAMEPLAY_BAN,
                TargetAvatar = avatarID,
                Value = untilDate,
                Message = message
            });
            Callbacks.Add(callback);
        }

        public void SetMayor(uint nhoodID, uint avatarID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.FORCE_MAYOR,
                TargetAvatar = avatarID,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void DeleteRate(uint rateID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.DELETE_RATE,
                Value = rateID
            });
            Callbacks.Add(callback);
        }

        public void AcceptNominations(uint nhoodID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.CAN_RUN,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void BeginNominations(uint nhoodID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.CAN_NOMINATE,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void BeginVoting(uint nhoodID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.CAN_VOTE,
                TargetNHood = nhoodID
            });
            Callbacks.Add(callback);
        }

        public void PretendDate(uint date, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.PRETEND_DATE,
                Value = date
            });
            Callbacks.Add(callback);
        }

        public void BeginFreeVote(uint nhoodID, Callback<NhoodResponseCode> callback)
        {
            if (Blocked) return;
            Blocked = true;
            ConnectionReg.MakeRequest(new NhoodRequest()
            {
                Type = NhoodRequestType.CAN_FREE_VOTE
            });
            Callbacks.Add(callback);
        }

        private void ResolveCallbacks(NhoodResponseCode code)
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

            if (data is NhoodResponse)
            {
                var response = data as NhoodResponse;
                if (response.Code == NhoodResponseCode.NHOOD_GAMEPLAY_BAN)
                {
                    //print message and end date
                    errorTitle = GameFacade.Strings.GetString("f117", "1");
                    errorBody = GameFacade.Strings.GetString("f117", ((int)response.Code + 1).ToString(),
                        new string[] {
                            ClientEpoch.DHMRemaining(response.BanEndDate),
                            response.Message
                        });
                }
                else if (response.Code == NhoodResponseCode.UNKNOWN_ERROR)
                {
                    errorTitle = GameFacade.Strings.GetString("f117", "1");
                    errorBody = GameFacade.Strings.GetString("f117", "28");
                    if (response.Message != "")
                    {
                        errorBody += "\n\n" + response.Message;
                    }
                }
                else
                {
                    errorTitle = GameFacade.Strings.GetString("f117", "1");
                    errorBody = GameFacade.Strings.GetString("f117", ((int)response.Code + 1).ToString());
                }
                LastMessage = response.Message;
                ResolveCallbacks(response.Code);
            }
            else
            {
                ResolveCallbacks(NhoodResponseCode.UNKNOWN_ERROR);
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
                            ResolveCallbacks(NhoodResponseCode.CANCEL);
                        }
                        if (BlockingDialog != null)
                        {
                            UIScreen.RemoveDialog(BlockingDialog);
                            BlockingDialog = null;
                        }
                        break;

                    case "ActionInput":
                        //show blocking dialog for this action
                        var req = ConnectionReg.CurrentRequest;
                        switch (req.Type)
                        {
                            case NhoodRequestType.CAN_NOMINATE:
                                //nomination dialog
                                //12 - 16
                                var nomCont = new UINominationSelectContainer(ConnectionReg.CandidateList);
                                BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("f118", "12"),
                                    Message = GameFacade.Strings.GetString("f118", "13", new string[] { "your neighborhood" }),
                                    Width = 440,
                                    GenericAddition = nomCont,
                                    Buttons = new UIAlertButton[] {
                                        new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                                            var newReq = nomCont.GetRequest(req);
                                            if (newReq != null) {
                                                UIAlert.YesNo(
                                                    GameFacade.Strings.GetString("f118", "14"),
                                                    GameFacade.Strings.GetString("f118", "15", new string[] { nomCont.SelectedCandidate.Name }),
                                                    true,
                                                    (result) =>
                                                    {
                                                        if (result) ConnectionReg.MakeRequest(newReq);
                                                    }
                                                    );
                                            }
                                            else
                                            {
                                                //tell user they should select a nominee
                                            }
                                        }, GameFacade.Strings.GetString("f118", "20")),
                                        new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => {
                                            ConnectionReg.AsyncReset();
                                        })
                                    }
                                }, false);
                                BlockingDialog.Opacity = 1;
                                break;
                            case NhoodRequestType.CAN_VOTE:
                                //voting dialog
                                BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("f118", "2"),
                                    Message = GameFacade.Strings.GetString("f118", "3"),
                                    Width = 500,

                                    Buttons = UIAlertButton.Ok((btn) =>
                                    {
                                        GameScreen.RemoveDialog(BlockingDialog);
                                        var votingCont = new UIVoteContainer(ConnectionReg.CandidateList);
                                        votingCont.OnVote += (id) =>
                                        {
                                            if (id == 0) ConnectionReg.AsyncReset();
                                            else
                                            {
                                                UIAlert.YesNo(
                                                    GameFacade.Strings.GetString("f118", "9"),
                                                    GameFacade.Strings.GetString("f118", "10", new string[] { votingCont.SelectedName }),
                                                    true,
                                                    (result) =>
                                                    {
                                                        if (result) ConnectionReg.MakeRequest(votingCont.MakeRequest(req));
                                                    }
                                                    );
                                            }
                                        };
                                        BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                        {
                                            Title = GameFacade.Strings.GetString("f118", "4"),
                                            Message = GameFacade.Strings.GetString("f118", "5"),
                                            Width = 600,
                                            GenericAddition = votingCont,
                                            Buttons = new UIAlertButton[0]
                                        }, false);

                                        votingCont.InjectClose();

                                        BlockingDialog.Opacity = 1;
                                    })
                                    }, true);
                                break;

                            case NhoodRequestType.CAN_RATE:
                                //rating dialog

                                var ratingCont = new UIRatingContainer(true);
                                BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("f115", "53"),
                                    Message = GameFacade.Strings.GetString("f115", "54"),
                                    Width = 450,
                                    GenericAddition = ratingCont,
                                    Buttons = new UIAlertButton[] {
                                                new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                                                    ConnectionReg.MakeRequest(ratingCont.GetRequest(req));
                                                }),
                                                new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => {
                                                    ConnectionReg.AsyncReset();
                                                })
                                            }
                                }, true);
                                BlockingDialog.Opacity = 1;
                                break;

                            case NhoodRequestType.CAN_RUN:
                                //run dialog
                                var runCont = new UIRatingContainer(false);
                                BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("f118", "17"),
                                    Message = GameFacade.Strings.GetString("f118", "18"),
                                    Width = 450,
                                    GenericAddition = runCont,
                                    Buttons = new UIAlertButton[] {
                                        new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                                            ConnectionReg.MakeRequest(runCont.GetRunRequest(req));
                                        }),
                                        new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => {
                                            ConnectionReg.AsyncReset();
                                        })
                                    }
                                }, true);

                                BlockingDialog.Opacity = 1;
                                break;

                            case NhoodRequestType.CAN_FREE_VOTE:
                                //free vote dialog

                                var freeCont = new UINominationSelectContainer(ConnectionReg.CandidateList, true);
                                BlockingDialog = UIScreen.GlobalShowAlert(new UIAlertOptions()
                                {
                                    Title = GameFacade.Strings.GetString("f118", "25"),
                                    Message = GameFacade.Strings.GetString("f118", "26"),
                                    Width = 440,
                                    GenericAddition = freeCont,
                                    Buttons = new UIAlertButton[] {
                                        new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                                            var newReq = freeCont.GetRequest(req);
                                            if (newReq != null) {
                                                UIAlert.YesNo(
                                                    GameFacade.Strings.GetString("f118", "27"),
                                                    GameFacade.Strings.GetString("f118", "28", new string[] { freeCont.SelectedCandidate.Name }),
                                                    true,
                                                    (result) =>
                                                    {
                                                        if (result) ConnectionReg.MakeRequest(newReq);
                                                    }
                                                    );
                                            }
                                            else
                                            {
                                                //tell user they should select a neighborhood
                                            }
                                        }, GameFacade.Strings.GetString("f118", "29")),
                                        new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => {
                                            ConnectionReg.AsyncReset();
                                        })
                                    }
                                }, false);
                                BlockingDialog.Opacity = 1;
                                break;

                            default:
                                //something went terribly wrong - we don't have any dialog to handle this.
                                ConnectionReg.AsyncReset();
                                break;
                        }
                        break;
                    case "ActionSuccess":
                        LastMessage = ((NhoodResponse)data).Message;
                        ResolveCallbacks(NhoodResponseCode.SUCCESS);
                        break;
                }
            });
        }
    }
}
