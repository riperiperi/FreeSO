using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Server.Protocol.Electron.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class JoinLotProgressController
    {
        private UIJoinLotProgress View;

        public JoinLotProgressController(UIJoinLotProgress view, JoinLotRegulator regulator)
        {
            this.View = view;

            regulator.OnError += Regulator_OnError;
            regulator.OnTransition += Regulator_OnTransition;
        }

        private void Regulator_OnError(object data)
        {
            //UIScreen.RemoveDialog(View);

            var errorTitle = GameFacade.Strings.GetString("211", "45");
            var errorBody = GameFacade.Strings.GetString("211", "45");

            if (data is FindLotResponseStatus)
            {
                var status = (FindLotResponseStatus)data;

                switch (status)
                {
                    case FindLotResponseStatus.NOT_OPEN:
                    case FindLotResponseStatus.NOT_PERMITTED_TO_OPEN:
                        errorTitle = GameFacade.Strings.GetString("211", "7");
                        errorBody = GameFacade.Strings.GetString("211", "8");
                        break;
                    case FindLotResponseStatus.NO_CAPACITY:
                        errorTitle = GameFacade.Strings.GetString("211", "11");
                        errorBody = GameFacade.Strings.GetString("211", "12");
                        break;
                    case FindLotResponseStatus.CLAIM_FAILED:
                        errorTitle = GameFacade.Strings.GetString("211", "45");
                        errorBody = GameFacade.Strings.GetString("211", "41");
                        break;
                    default:
                        break;
                }
            }

            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = errorTitle,
                Message = errorBody,
                Buttons = UIAlertButton.Ok(x => {
                    UIScreen.RemoveDialog(View);
                    UIScreen.RemoveDialog(alert);
                })
            }, true);
        }

        private void Regulator_OnTransition(string state, object data)
        {
            var progress = 0;

            /**22 ^Property location determined...^
                23 ^Verifying permissions...^
                24 ^Attempting to join a property...^
                25 ^Property joined successfully. Requesting property data...^
                26 ^Retrieving property data...^
                27 ^Property data retrieved. Entering property...^**/

            switch (state)
            {
                case "Floating":
                    UIScreen.RemoveDialog(View);
                    break;

                case "Start":
                    UIScreen.GlobalShowDialog(View, true);
                    break;

                case "FindLot":
                    break;

                case "FindLotResponse":
                    progress = 1;
                    break;
            }


            var progressPercent = (((float)progress) / 6.0f);
            View.Progress = progressPercent;
            switch (progress)
            {
                case 0:
                    View.ProgressCaption = GameFacade.Strings.GetString("211", "4");
                    break;
                default:
                    View.ProgressCaption = GameFacade.Strings.GetString("211", (21 + progress).ToString());
                    break;
            }
        }
    }
}
