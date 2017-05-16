using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;

namespace FSO.Client.UI.Panels.EODs
{
    class UITimerPluginEOD : UIEOD
    {
        public UIScript Script;

        // buttons
        public UIButton PlayButton { get; set; }
        public UIButton PauseButton { get; set; }
        public UIButton MinUpButton { get; set; }
        public UIButton SecUpButton { get; set; }
        public UIButton MinDownButton { get; set; }
        public UIButton SecDownButton { get; set; }
        public UIButton StopwatchButton { get; set; }
        public UIButton CountdownButton { get; set; }

        // images
        public UIImage SubpanelBackground { get; set; }

        // textedits
        public UITextEdit MinTextEntry { get; set; }
        public UITextEdit SecTextEntry { get; set; }

        // text
        public UILabel ColonStaticText { get; set; }

        private bool IsRunning;
        private bool IsInStopwatchMode;
        private bool MinutesWereChanged;
        private bool SecondsWereChanged;
        private byte CurrentMinutes;
        private byte CurrentSeconds;

        public UITimerPluginEOD(UIEODController controller) : base(controller)
        {
            Script = RenderScript("timereod.uis");

            // add bg
            SubpanelBackground = Script.Create<UIImage>("SubpanelBackground");
            AddAt(0, SubpanelBackground);

            // add listseners
            PlayButton.OnButtonClick += PlayButtonClickedHandler;
            PauseButton.OnButtonClick += PauseButtonClickedHandler;
            MinUpButton.OnButtonClick += MinUpButtonClickedHandler;
            MinDownButton.OnButtonClick += MinDownButtonClickedHandler;
            SecUpButton.OnButtonClick += SecUpButtonClickedHandler;
            SecDownButton.OnButtonClick += SecDownButtonClickedHandler;
            StopwatchButton.OnButtonClick += StopwatchButtonClickedHandler;
            CountdownButton.OnButtonClick += CountdownButtonClickedHandler;

            BinaryHandlers["Timer_Show"] = TimerInitHandler;
            BinaryHandlers["Timer_Off"] = OffHandler;
            PlaintextHandlers["Timer_Update"] = UpdateTimeHandler;
        }

        public override void OnClose()
        {
            base.OnClose();
            Send("Timer_Close", "");
        }

        private void UpdateTime()
        {
            // update the UI
            MinTextEntry.CurrentText = (CurrentMinutes > 9) ? CurrentMinutes + "" : "0" + CurrentMinutes;
            SecTextEntry.CurrentText = (CurrentSeconds > 9) ? CurrentSeconds + "" : "0" + CurrentSeconds;

            // update the server
            Send("Timer_Set", new byte[] { CurrentMinutes, CurrentSeconds } );
        }

        private void TimerInitHandler(string evt, Byte[] args)
        {
            if ((args == null) || (args.Length < 3))
                return;
            
            // is in stopwatch mode
            if (args[1] == 1)
            {
                IsInStopwatchMode = true;
                StopwatchButton.ForceState = 1;
                StopwatchButton.Disabled = true;
                CountdownButton.ForceState = 0;
                CountdownButton.Disabled = false;
            }
            else
            {
                IsInStopwatchMode = false;
                StopwatchButton.ForceState = 0;
                StopwatchButton.Disabled = false;
                CountdownButton.ForceState = 1;
                CountdownButton.Disabled = true;
            }

            // is running
            if (args[0] == 1)
            {
                IsRunning = true;
                PlayButton.Disabled = true;
                DisableMinutesTextEdit();
                DisableSecondsTextEdit();
                CountdownButton.Disabled = true;
                StopwatchButton.Disabled = true;
                HideTextFields();
            }
            else
            {
                IsRunning = false;
                PauseButton.Disabled = true;
                EnableMinutesTextEdit();
                EnableSecondsTextEdit();
                ShowTextFields();
            }

            // minutes
            CurrentMinutes = (args[2] < 100) ? args[2] : (byte)0;
            MinTextEntry.CurrentText = (CurrentMinutes > 9) ? CurrentMinutes + "" : "0" + CurrentMinutes;
            MinTextEntry.Alignment = TextAlignment.Center;
            // seconds
            CurrentSeconds = (args[3] < 60) ? args[3] : (byte)0;
            SecTextEntry.CurrentText = (CurrentSeconds > 9) ? CurrentSeconds + "" : "0" + CurrentSeconds;
            SecTextEntry.Alignment = TextAlignment.Center;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Height = EODHeight.Normal,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.None,
            });
        }

        private void OffHandler(string evt, byte[] state)
        {
            if (state[0] == 0)
            {
                IsInStopwatchMode = false;
                CountdownButton.Disabled = true;
                CountdownButton.ForceState = 1;
                StopwatchButton.ForceState = 0;
                StopwatchButton.Disabled = false;
                IsRunning = false;
                PlayButton.Disabled = false;
                PauseButton.Disabled = true;
                EnableSecondsTextEdit();
                EnableMinutesTextEdit();
                ShowTextFields();
            }
        }

        private void UpdateTimeHandler(string evt, string newTimeString)
        {
            var split = newTimeString.Split(':');
            if (split.Length != 2) return;

            // try to parse the new times, 0 defaults if it cannot be done
            byte minutes = 100;
            byte seconds = 60;
            if (Byte.TryParse(split[0], out minutes))
                CurrentMinutes = minutes;
            else
                CurrentMinutes = 0;
            if (Byte.TryParse(split[1], out seconds))
                CurrentSeconds = seconds;
            else
                CurrentSeconds = 0;
            UpdateTime();
        }

        private void PlayButtonClickedHandler(UIElement target)
        {
            PlayButton.Disabled = true;
            StopwatchButton.Disabled = true;
            CountdownButton.Disabled = true;
            PauseButton.Disabled = false;
            DisableMinutesTextEdit();
            DisableSecondsTextEdit();
            HideTextFields();
            if ((MinutesWereChanged) || (SecondsWereChanged))
            {
                UpdateTime();
                MinutesWereChanged = false;
                SecondsWereChanged = false;
            }
            if (!IsRunning)
            {
                Send("Timer_IsRunning_Change", new Byte[] { 1 });
            }
            IsRunning = true;
        }

        private void PauseButtonClickedHandler(UIElement target)
        {
            PauseButton.Disabled = true;
            if (IsRunning)
            {
                Send("Timer_IsRunning_Change", new Byte[] { 0 });
            }
            IsRunning = false;
            PlayButton.Disabled = false;
            if (IsInStopwatchMode)
                CountdownButton.Disabled = false;
            else
                StopwatchButton.Disabled = false;
            EnableMinutesTextEdit();
            EnableSecondsTextEdit();
            ShowTextFields();
        }

        private void MinUpButtonClickedHandler(UIElement target)
        {
            DisableMinutesTextEdit();
            if (CurrentMinutes < 99)
            {
                CurrentMinutes++;
                UpdateTime();
            }
            EnableMinutesTextEdit();
        }

        private void SecUpButtonClickedHandler(UIElement target)
        {
            DisableSecondsTextEdit();
            if (CurrentSeconds == 59)
            {
                CurrentSeconds = 0;
                if (CurrentMinutes < 99)
                    CurrentMinutes++;
            }
            else
            {
                CurrentSeconds++;
            }
            UpdateTime();
            EnableSecondsTextEdit();
        }

        private void MinDownButtonClickedHandler(UIElement target)
        {
            DisableMinutesTextEdit();
            if (CurrentMinutes > 0)
            {
                CurrentMinutes--;
                UpdateTime();
            }
            EnableMinutesTextEdit();
        }

        private void SecDownButtonClickedHandler(UIElement target)
        {
            DisableSecondsTextEdit();
            if (CurrentSeconds > 0)
            {
                CurrentSeconds--;
                UpdateTime();
            }
            EnableSecondsTextEdit();
        }

        private void StopwatchButtonClickedHandler(UIElement target)
        {
            StopwatchButton.Disabled = true;
            if (!IsInStopwatchMode)
            {
                Send("Timer_State_Change", new byte[] { 1 });
                IsInStopwatchMode = true;
            }
            StopwatchButton.ForceState = 1;
            CountdownButton.ForceState = 0;
            CountdownButton.Disabled = false;
            if ((MinutesWereChanged) || (SecondsWereChanged))
            {
                UpdateTime();
                MinutesWereChanged = false;
                SecondsWereChanged = false;
            }
        }

        private void CountdownButtonClickedHandler(UIElement target)
        {
            CountdownButton.Disabled = true;
            if (IsInStopwatchMode)
            {
                Send("Timer_State_Change", new byte[] { 0 });
                IsInStopwatchMode = false;
            }
            CountdownButton.ForceState = 1;
            StopwatchButton.ForceState = 0;
            StopwatchButton.Disabled = false;
            if ((MinutesWereChanged) || (SecondsWereChanged))
            {
                UpdateTime();
                MinutesWereChanged = false;
                SecondsWereChanged = false;
            }
        }

        private void MinTextEntryChangeHandler(UIElement target)
        {
            byte minutes = 100;
            if (Byte.TryParse(MinTextEntry.CurrentText, out minutes))
            {
                if (minutes < 100)
                    CurrentMinutes = minutes;
                else
                    CurrentMinutes = 99;
                MinutesWereChanged = true;
            }
        }

        private void SecTextEntryChangeHandler(UIElement target)
        {
            byte seconds = 60;
            if (Byte.TryParse(SecTextEntry.CurrentText, out seconds))
            {
                if (seconds < 60)
                    CurrentSeconds = seconds;
                else
                    CurrentSeconds = 59;
                SecondsWereChanged = true;
            }
        }

        private void EnableMinutesTextEdit()
        {
            MinTextEntry.Mode = UITextEditMode.Editor;
            MinTextEntry.OnChange += MinTextEntryChangeHandler;
            MinUpButton.Disabled = false;
            MinDownButton.Disabled = false;
        }

        private void DisableMinutesTextEdit()
        {
            MinTextEntry.Mode = UITextEditMode.ReadOnly;
            MinTextEntry.OnChange -= MinTextEntryChangeHandler;
            MinUpButton.Disabled = true;
            MinDownButton.Disabled = true;
        }

        private void EnableSecondsTextEdit()
        {
            SecTextEntry.Mode = UITextEditMode.Editor;
            SecTextEntry.OnChange += SecTextEntryChangeHandler;
            SecUpButton.Disabled = false;
            SecDownButton.Disabled = false;
        }

        private void DisableSecondsTextEdit()
        {
            SecTextEntry.Mode = UITextEditMode.ReadOnly;
            SecTextEntry.OnChange -= SecTextEntryChangeHandler;
            SecUpButton.Disabled = true;
            SecDownButton.Disabled = true;
        }

        private void ShowTextFields()
        {
            MinTextEntry.Visible = true;
            SecTextEntry.Visible = true;
            ColonStaticText.Visible = true;
        }

        private void HideTextFields()
        {
            MinTextEntry.Visible = false;
            SecTextEntry.Visible = false;
            ColonStaticText.Visible = false;
        }

    }
}
