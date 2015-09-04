using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class PersonSelectionEditController : IDisposable
    {
        private PersonSelectionEdit View;
        private CreateASimRegulator CASRegulator;

        public PersonSelectionEditController(PersonSelectionEdit view, CreateASimRegulator casRegulator)
        {
            this.View = view;
            this.CASRegulator = casRegulator;

            CASRegulator.OnTransition += CASRegulator_OnTransition;
        }
        
        private void CASRegulator_OnTransition(string state, object data)
        {
            switch (state){
                case "Idle":
                    break;
                case "CreateSim":
                    break;
                case "Waiting":
                    //Show waiting dialog
                    View.ShowCreationProgressBar(true);
                    break;
                case "Error":
                    View.ShowCreationProgressBar(false);
                    break;
                case "Success":
                    //Connect to the city with our new avatar
                    var response = (CreateASimResponse)data;
                    GameFacade.Controller.ConnectToCity(null, response.NewAvatarId);
                    break;
            }
        }

        public void Create(){
            var skinTone = Server.Protocol.Voltron.Model.SkinTone.LIGHT;
            switch (View.AppearanceType)
            {
                case Vitaboy.AppearanceType.Medium:
                    skinTone = Server.Protocol.Voltron.Model.SkinTone.MEDIUM;
                    break;
                case Vitaboy.AppearanceType.Dark:
                    skinTone = Server.Protocol.Voltron.Model.SkinTone.DARK;
                    break;
            }

            var packet = new RSGZWrapperPDU
            {
                BodyOutfitId = (uint)(View.BodyOutfitId >> 32),
                HeadOutfitId = (uint)(View.HeadOutfitId >> 32),
                Name = View.Name,
                Description = View.Description,
                Gender = View.Gender == Gender.Male ? Server.Protocol.Voltron.Model.Gender.MALE : Server.Protocol.Voltron.Model.Gender.FEMALE,
                SkinTone = skinTone
            };

            CASRegulator.CreateSim(packet);
        }

        public void Cancel(){
            if (CASRegulator.CurrentState.Name == "Idle")
            {
                //Cant cancel while cas in progress
                GameFacade.Controller.Disconnect();
            }
        }

        public void Dispose()
        {
            CASRegulator.OnTransition -= CASRegulator_OnTransition;
        }
    }
}
