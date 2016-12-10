using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.EODs.Archetypes;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIScoreboardEOD : UIBasicEOD
    {
        private UIImage Background;
        private UIImage ColorSelector;

        public UIButton ColorRed { get; set; }
        public UIButton ColorBlue { get; set; }
        public UIButton ColorYellow { get; set; }
        public UIButton ColorGreen { get; set; }
        public UIButton ColorOrange { get; set; }
        public UIButton ColorPurple { get; set; }
        public UIButton ColorWhite { get; set; }
        public UIButton ColorBlack { get; set; }

        public UIButton ColorLHS { get; set; }
        public UIButton ColorRHS { get; set; }
        public UILabel Loading { get; set; }

        public UITextEdit ScoreTextEntryLHS { get; set; }
        public UITextEdit ScoreTextEntryRHS { get; set; }

        public UIButton SpinnerIncreaseLHS { get; set; }
        public UIButton SpinnerDecreaseLHS { get; set; }
        public UIButton SpinnerIncreaseRHS { get; set; }
        public UIButton SpinnerDecreaseRHS { get; set; }

        public UIButton[] ColorButtons;

        private VMEODScoreboardTeam SelectedTeam;


        public UIScoreboardEOD(UIEODController controller) : base(controller, "scoreboard", "scoreboardeod.uis")
        {
        }


        /**
         * EOD callbacks
         */
        private void SetState(string evt, byte[] data){
            var state = new VMEODScoreboardData(data);

            UpdateColor(VMEODScoreboardTeam.LHS, state.LHSColor);
            UpdateColor(VMEODScoreboardTeam.RHS, state.RHSColor);

            ScoreTextEntryLHS.CurrentText = "" + state.LHSScore;
            ScoreTextEntryRHS.CurrentText = "" + state.RHSScore;

            if (Loading.Visible){
                ShowLoading(false);
            }
        }

        protected override void InitEOD()
        {
            base.InitEOD();
            BinaryHandlers["scoreboard_state"] = SetState;
        }

        protected override void InitUI()
        {
            base.InitUI();

            ColorSelector = Script.Create<UIImage>("ColorSelector");
            AddAt(0, ColorSelector);

            Background = Script.Create<UIImage>("UIBackground");
            AddAt(0, Background);

            ColorButtons = new UIButton[]{
                ColorRed,
                ColorBlue,
                ColorYellow,
                ColorGreen,
                ColorOrange,
                ColorPurple,
                ColorWhite,
                ColorBlack
            };

            foreach(var btn in ColorButtons){
                btn.OnButtonClick += ColorButton_OnButtonClick;
            }

            ShowColorPicker(false);
            ShowLoading(true);

            ColorRHS.OnButtonClick += x => ChangeColor(VMEODScoreboardTeam.RHS);
            ColorLHS.OnButtonClick += x => ChangeColor(VMEODScoreboardTeam.LHS);

            SpinnerDecreaseLHS.OnButtonClick += x => Spinner(VMEODScoreboardTeam.LHS, -1);
            SpinnerIncreaseLHS.OnButtonClick += x => Spinner(VMEODScoreboardTeam.LHS, 1);
            SpinnerDecreaseRHS.OnButtonClick += x => Spinner(VMEODScoreboardTeam.RHS, -1);
            SpinnerIncreaseRHS.OnButtonClick += x => Spinner(VMEODScoreboardTeam.RHS, 1);

            ScoreTextEntryLHS.MaxLines = 1;
            ScoreTextEntryRHS.MaxLines = 1;
            ScoreTextEntryLHS.MaxChars = 3;
            ScoreTextEntryRHS.MaxChars = 3;

            ScoreTextEntryLHS.OnChange += x => Debounce("lhs", () => SetScore(VMEODScoreboardTeam.LHS, ScoreTextEntryLHS.CurrentText));
            ScoreTextEntryRHS.OnChange += x => Debounce("rhs", () => SetScore(VMEODScoreboardTeam.RHS, ScoreTextEntryRHS.CurrentText));
        }

        private void SetScore(VMEODScoreboardTeam team, string text)
        {
            short value;
            if (!short.TryParse(text, out value)) { return; }

            if (value < 0) { value = 0; }
            if (value > 999) { value = 999; }

            Send("scoreboard_setscore", team.ToString() + "," + value);
        }

        private void Spinner(VMEODScoreboardTeam team, short difference)
        {
            Send("scoreboard_updatescore", team.ToString() + "," + difference);
        }

        private void UpdateColor(VMEODScoreboardTeam team, VMEODScoreboardColor color)
        {
            //Update the UI
            var targetButton = ColorLHS;
            if (team == VMEODScoreboardTeam.RHS) { targetButton = ColorRHS; }
            Script.ApplyControlProperties(targetButton, "ColorButton" + ((byte)color));
        }

        private void ColorButton_OnButtonClick(UIElement button)
        {
            var index = Array.IndexOf(ColorButtons, button);
            if (index == -1 || index > 7) { return; }
            var color = (VMEODScoreboardColor)index;

            Send("scoreboard_updatecolor", SelectedTeam.ToString() + "," + color.ToString());
            UpdateColor(SelectedTeam, color);
            ShowColorPicker(false);
        }

        private void ChangeColor(VMEODScoreboardTeam team)
        {
            SelectedTeam = team;
            ShowColorPicker(true);
        }

        protected override EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Normal,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            };
        }

        private void ShowLoading(bool loading)
        {
            Loading.Visible = loading;

            ColorRHS.Visible =
                ColorLHS.Visible =
                Background.Visible =
                ScoreTextEntryLHS.Visible =
                ScoreTextEntryRHS.Visible =
                SpinnerDecreaseLHS.Visible =
                SpinnerDecreaseRHS.Visible =
                SpinnerIncreaseLHS.Visible =
                SpinnerIncreaseRHS.Visible = !loading;
        }

        private void ShowColorPicker(bool show)
        {
            ColorSelector.Visible = show;
            foreach(var btn in ColorButtons){
                btn.Visible = show;
            }
        }
    }
}
