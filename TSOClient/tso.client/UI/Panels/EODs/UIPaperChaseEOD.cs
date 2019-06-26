using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
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
        private UIScript Script;

        //EOD State
        private VMEODPaperChaseState State;
        private UIEODLobby Lobby;

        //Controls
        public UIButton AButton { get; set; }
        public UIButton BButton { get; set; }
        public UIButton CButton { get; set; }

        public UILabel kLetterEntryBody { get; set; }
        public UILabel kLetterEntryMech { get; set; }
        public UILabel kLetterEntryLogic { get; set; }

        public UILabel kPrevText { get; set; }
        public UILabel kPrevCorrect { get; set; }

        public UILabel kLetterPrevBody { get; set; }
        public UILabel kLetterPrevMech { get; set; }
        public UILabel kLetterPrevLogic { get; set; }

        //Chrome
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
            Script = this.RenderScript("paperchaseeod.uis");
            background = Script.Create<UIImage>("UIBackground");
            AddAt(0, background);

            PersonBG1 = Script.Create<UIImage>("playerPos1");
            PersonBG2 = Script.Create<UIImage>("playerPos2");
            PersonBG3 = Script.Create<UIImage>("playerPos3");
            PersonBG1.Texture = imagePlayer;
            PersonBG2.Texture = imagePlayer;
            PersonBG3.Texture = imagePlayer;
            Add(PersonBG1);
            Add(PersonBG2);
            Add(PersonBG3);

            labelStation1.Alignment = TextAlignment.Left;
            labelStation2.Alignment = TextAlignment.Left;
            labelStation3.Alignment = TextAlignment.Left;

            AButton.OnButtonClick += ChooseLetter;
            BButton.OnButtonClick += ChooseLetter;
            CButton.OnButtonClick += ChooseLetter;

            Lobby = new UIEODLobby(this, 3)
                .WithPlayerUI(new UIEODLobbyPlayer(0, PersonBG1, labelStation1))
                .WithPlayerUI(new UIEODLobbyPlayer(1, PersonBG2, labelStation2))
                .WithPlayerUI(new UIEODLobbyPlayer(2, PersonBG3, labelStation3))
                .WithCaptionProvider((player, avatar) => {
                    if(avatar == null)
                    {
                        return "";
                    }

                    switch (player.Slot+1)
                    {
                        case (int)VMEODPaperChaseSlots.Body:
                            return Script.GetString("strBody") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.BodySkill) / 100;
                        case (int)VMEODPaperChaseSlots.Mechanical:
                            return Script.GetString("strMech") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.MechanicalSkill) / 100;
                        case (int)VMEODPaperChaseSlots.Logic:
                            return Script.GetString("strLogic") + avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.LogicSkill) / 100;
                    }

                    return "";
                });
            Add(Lobby);
        }

        private void ChooseLetter(UIElement button)
        {
            var buttons = new UIButton[] { AButton, BButton, CButton  };
            var letterButton = Array.IndexOf(buttons, button);
            if (letterButton == -1) { return; }

            var letter = VMEODPaperChaseLetters.All[letterButton];

            foreach (var btn in buttons) btn.Disabled = true;
            Send("paperchase_chooseletter", letter.ToString());
        }

        private void InitEOD()
        {
            State = VMEODPaperChaseState.Lobby;
            DigestState();

            PlaintextHandlers["paperchase_show"] = Show;
            PlaintextHandlers["paperchase_players"] = Lobby.UpdatePlayers;
            PlaintextHandlers["paperchase_state"] = SetState;
            PlaintextHandlers["paperchase_letters"] = SetLetters;
            PlaintextHandlers["paperchase_result"] = SetResult;
        }

        private void SetResult(string evt, string body)
        {
            var matches = int.Parse(body);
            if(matches == 3){
                SetTip(Script.GetString("Correct2"));
            }else{
                SetTip(matches + Script.GetString("Checkresult"));
            }
        }

        private void SetLetters(string evt, string body)
        {
            var letters = body.Split('\n');
            if (letters.Length != 7) { return; }

            var currentLetters = new UILabel[] { kLetterEntryBody, kLetterEntryMech, kLetterEntryLogic };
            var previousLetters = new UILabel[] { kLetterPrevBody, kLetterPrevMech, kLetterPrevLogic };

            for(var i=0; i < 3; i++){
                var letter = (short)-1;
                short.TryParse(letters[i], out letter);
                currentLetters[i].Caption = GetLetterString(letter);
            }

            for (var i = 0; i < 3; i++){
                var letter = (short)-1;
                short.TryParse(letters[i+3], out letter);
                previousLetters[i].Caption = GetLetterString(letter);
            }

            var previousMatches = (short)-1;
            short.TryParse(letters[6], out previousMatches);

            if(previousMatches != -1)
            {
                kPrevText.Caption = Script.GetString("Previous");
                kPrevCorrect.Caption = previousMatches + "/3";
            }
            else
            {
                kPrevText.Caption = "";
                kPrevCorrect.Caption = "";
            }
        }

        private string GetLetterString(short letter)
        {
            switch (letter)
            {
                case VMEODPaperChaseLetters.A:
                    return Script.GetString("AString");
                case VMEODPaperChaseLetters.B:
                    return Script.GetString("BString");
                case VMEODPaperChaseLetters.C:
                    return Script.GetString("CString");
            }
            return "";
        }

        private void SetState(string evt, string body)
        {
            State = (VMEODPaperChaseState)int.Parse(body);
            DigestState();
        }

        private void DigestState()
        {
            AButton.Disabled = State != VMEODPaperChaseState.WaitingForThreeLetters;
            BButton.Disabled = State != VMEODPaperChaseState.WaitingForThreeLetters;
            CButton.Disabled = State != VMEODPaperChaseState.WaitingForThreeLetters;

            switch (State)
            {
                case VMEODPaperChaseState.Lobby:
                    SetTip(Script.GetString("Waitslots"));
                    break;
                case VMEODPaperChaseState.WaitingForThreeLetters:
                    SetTip(Script.GetString("Waiting"));
                    break;
                case VMEODPaperChaseState.CheckingResult:
                    SetTip(Script.GetString("Checking"));
                    break;
            }
        }

        public void Show(string evt, string txt)
        {
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Normal,
                Length =  EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.Long
            });
        }

        public override void OnClose()
        {
            CloseInteraction();
            base.OnClose();
        }
    }
}
