using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Utils;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIPaperChaseEOD : UIEOD
    {
        private UIEODLobby Lobby;

        private UIImage background;

        //Avatars
        public UIImage PersonBG1;
        public UIImage PersonBG2;
        public UIImage PersonBG3;

        public UILabel labelStation1 { get; set; }
        public UILabel labelStation2 { get; set; }
        public UILabel labelStation3 { get; set; }

        //Textures
        public Texture2D imagePlayer { get; set; }


        public UIPaperChaseEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            InitEOD();
        }

        private void InitUI()
        {
            var script = this.RenderScript("paperchaseeod.uis");
            background = script.Create<UIImage>("UIBackground");
            AddAt(0, background);

            PersonBG1 = script.Create<UIImage>("playerPos1");
            PersonBG2 = script.Create<UIImage>("playerPos2");
            PersonBG3 = script.Create<UIImage>("playerPos3");
            PersonBG1.Texture = imagePlayer;
            PersonBG2.Texture = imagePlayer;
            PersonBG3.Texture = imagePlayer;
            Add(PersonBG1);
            Add(PersonBG2);
            Add(PersonBG3);

            labelStation1.Alignment = TextAlignment.Left;
            labelStation2.Alignment = TextAlignment.Left;
            labelStation3.Alignment = TextAlignment.Left;

            Lobby = new UIEODLobby(this, 3)
                .WithPlayerUI(new UIEODLobbyPlayer(0, PersonBG1, labelStation1))
                .WithPlayerUI(new UIEODLobbyPlayer(1, PersonBG2, labelStation2))
                .WithPlayerUI(new UIEODLobbyPlayer(2, PersonBG2, labelStation2))
                .WithCaptionProvider((player, avatar) => {
                    if(avatar == null)
                    {
                        return "";
                    }

                    switch (player.Slot)
                    {
                        case (int)VMEODPaperChaseSlots.BODY:
                            return script.GetString("strBody") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.LogicSkill) / 100;
                        case (int)VMEODPaperChaseSlots.MECHANICAL:
                            return script.GetString("strMech") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.LogicSkill) / 100;
                        case (int)VMEODPaperChaseSlots.LOGIC:
                            return script.GetString("strLogic") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.LogicSkill) / 100;
                    }

                    return "";
                });
            Add(Lobby);
        }
        
        private void InitEOD()
        {
            PlaintextHandlers["paperchase_show"] = P_Show;
            PlaintextHandlers["paperchase_players"] = Lobby.UpdatePlayers;
        }

        public void P_Show(string evt, string txt)
        {
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Normal,
                Length = EODLength.Medium,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short
            });
        }
    }
}
