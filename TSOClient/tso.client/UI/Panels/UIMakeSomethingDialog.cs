using System;
using System.Collections.Generic;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// "Make Something" — the player-facing conversational authoring panel
    /// (PLAYER-LAYER-DESIGN.md §2). Pure code, no .uis script: FreeSO's Buy Mode/UCP panels
    /// are laid out by proprietary .uis assets that ship as original EA content and aren't
    /// source-controlled in this repo, so a brand-new panel like this one follows UIModMenu's
    /// idiom instead — a UIDialog subclass built entirely from generic controls already in
    /// FSO.Client.UI.Controls. No new art assets required.
    ///
    /// Layout: scrolling narration log (reusing UIChatDialog's history-box pattern: a
    /// UITextEdit with MaxLines + BBCodeEnabled + AttachSlider) on top, a progress bar
    /// (UILoginProgress's Progress/ProgressCaption pattern) that only appears while the agent
    /// is working, and a single-line entry box + send button on the bottom (UIChatDialog's
    /// entry pattern, including OnEnterPress).
    ///
    /// Threading: per the approved seam, IMakeSomethingAgent implementations marshal their own
    /// callbacks onto the game thread before invoking them — this class does not call
    /// GameThread.NextUpdate itself.
    /// </summary>
    public class UIMakeSomethingDialog : UIDialog
    {
        UITextEdit HistoryText;
        UISlider HistorySlider;
        UIButton HistoryScrollUpButton;
        UIButton HistoryScrollDownButton;
        UIImage HistoryBackground;

        UIProgressBar Progress;
        UILabel ProgressLabel;

        UITextEdit EntryText;
        UIButton SendButton;
        UIImage EntryBackground;

        readonly List<string> Lines = new List<string>();
        IMakeSomethingAgent Agent;
        bool Working;

        public UIMakeSomethingDialog() : base(UIDialogStyle.Tall | UIDialogStyle.Close, true)
        {
            SetSize(400, 320);
            Caption = "Make Something";

            HistoryBackground = new UIImage(UITextBox.StandardBackground);
            HistoryBackground.Position = new Vector2(20, 40);
            HistoryBackground.SetSize(341, 150);
            Add(HistoryBackground);

            HistoryText = new UITextEdit();
            HistoryText.Position = new Vector2(29, 47);
            HistoryText.SetSize(HistoryBackground.Size.X - 19, HistoryBackground.Size.Y - 16);
            HistoryText.MaxLines = 200;
            HistoryText.BBCodeEnabled = true;
            Add(HistoryText);

            HistorySlider = new UISlider();
            HistorySlider.Position = new Vector2(HistoryBackground.Position.X + HistoryBackground.Size.X - 3, HistoryBackground.Position.Y + 10);
            HistorySlider.SetSize(HistorySlider.Size.X, HistoryBackground.Size.Y - 26);
            HistorySlider.MinValue = 0;
            Add(HistorySlider);

            HistoryScrollUpButton = new UIButton();
            HistoryScrollUpButton.Caption = "^";
            HistoryScrollUpButton.Position = new Vector2(HistorySlider.Position.X, HistoryBackground.Position.Y);
            Add(HistoryScrollUpButton);

            HistoryScrollDownButton = new UIButton();
            HistoryScrollDownButton.Caption = "v";
            HistoryScrollDownButton.Position = new Vector2(HistorySlider.Position.X, HistoryBackground.Position.Y + HistoryBackground.Size.Y - 14);
            Add(HistoryScrollDownButton);

            HistorySlider.AttachButtons(HistoryScrollUpButton, HistoryScrollDownButton, 1);
            HistoryText.AttachSlider(HistorySlider);

            ProgressLabel = new UILabel();
            ProgressLabel.Caption = "";
            ProgressLabel.Position = new Vector2(20, 200);
            Add(ProgressLabel);

            Progress = new UIProgressBar();
            Progress.Position = new Vector2(20, 218);
            Progress.SetSize(341, 20);
            Progress.Value = 0;
            Add(Progress);
            SetWorking(false);

            EntryBackground = new UIImage(UITextBox.StandardBackground);
            EntryBackground.Position = new Vector2(20, 260);
            EntryBackground.SetSize(280, 26);
            Add(EntryBackground);

            EntryText = new UITextEdit();
            EntryText.Position = new Vector2(29, 265);
            EntryText.SetSize(262, 17);
            EntryText.OnEnterPress += _ => Send();
            Add(EntryText);

            SendButton = new UIButton();
            SendButton.Caption = "Send";
            SendButton.Position = new Vector2(308, 260);
            SendButton.Width = 60;
            SendButton.OnButtonClick += _ => Send();
            Add(SendButton);

            CloseButton.OnButtonClick += _ => { Visible = false; };

            AppendLine("[color=#999999]Tell me what you'd like to make.[/color]");
        }

        public void SetAgent(IMakeSomethingAgent agent)
        {
            if (Agent != null)
            {
                Agent.OnNarration -= HandleNarration;
                Agent.OnObjectComplete -= HandleComplete;
                Agent.OnError -= HandleError;
            }
            Agent = agent;
            if (Agent != null)
            {
                Agent.OnNarration += HandleNarration;
                Agent.OnObjectComplete += HandleComplete;
                Agent.OnError += HandleError;
            }
        }

        void Send()
        {
            if (Working) return;
            var text = EntryText.CurrentText;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (Agent == null)
            {
                AppendLine("[color=#CC6666]Nothing's listening yet.[/color]");
                return;
            }

            AppendLine("[color=#8FBF8F]You: " + SanitizeBB(text) + "[/color]");
            EntryText.CurrentText = "";
            SetWorking(true);
            Agent.SendMessage(text);
        }

        void HandleNarration(string line)
        {
            AppendLine(line);
        }

        void HandleComplete(uint guid)
        {
            AppendLine("[color=#8FBF8F]Done! It's in your inventory now.[/color]");
            SetWorking(false);
        }

        void HandleError(string message)
        {
            AppendLine("[color=#CC6666]" + SanitizeBB(message) + "[/color]");
            SetWorking(false);
        }

        void SetWorking(bool working)
        {
            Working = working;
            Progress.Visible = working;
            ProgressLabel.Visible = working;
            SendButton.Disabled = working;
            if (!working) Progress.Value = 0;
        }

        void AppendLine(string bbLine)
        {
            Lines.Add(bbLine);
            if (Lines.Count > 200) Lines.RemoveAt(0);

            bool wasAtBottom = Math.Abs(HistoryText.VerticalScrollMax - HistoryText.VerticalScrollPosition) < 2;
            HistoryText.CurrentText = string.Join("\n", Lines);
            HistoryText.ComputeDrawingCommands();
            if (wasAtBottom) HistoryText.VerticalScrollPosition = HistoryText.VerticalScrollMax;
        }

        string SanitizeBB(string input)
        {
            return FSO.Common.Utils.BBCodeParser.SanitizeBB(input);
        }
    }
}
