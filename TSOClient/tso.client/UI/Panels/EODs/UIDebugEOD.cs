using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    /// <summary>
    /// Debug EOD that lets you choose all the options
    /// </summary>
    public class UIDebugEOD : UIEOD
    {
        private UIHBoxContainer Container;
        private EODLiveModeOpt Options;
        

        public UIDebugEOD(UIEODController controller) : base(controller)
        {
            Options = new EODLiveModeOpt();

            InitUI();
            SetMode();
        }

        private void InitUI()
        {
            Container = new UIHBoxContainer() {
                Spacing = 10
            };
            Add(Container);

            //Height
            AddRadioGroup("Height", "Height", new string[] { "Normal", "Tall", "Double Tall" }, new object[] { EODHeight.Normal, EODHeight.Tall, EODHeight.TallTall });

            //Width
            AddRadioGroup("Length", "Length", new string[] { "Full", "Medium", "Short" }, new object[] { EODLength.Full, EODLength.Medium, EODLength.Short });

            //Buttons
            AddRadioGroup("Buttons", "Buttons", new string[] { "0", "1", "2" }, new object[] { (byte)0, (byte)1, (byte)2 });

            //Timer
            AddRadioGroup("Timer", "Timer", new string[] { "None", "Normal", "Straight" }, new object[] { EODTimer.None, EODTimer.Normal, EODTimer.Straight });

            //Tips
            AddRadioGroup("Tips", "Tips", new string[] { "None", "Long", "Short" }, new object[] { EODTextTips.None, EODTextTips.Long, EODTextTips.Short });

            var other = new UIVBoxContainer();

            //Expandable
            AddCheckBox(other, "Expandable", "Expandable");

            Container.Add(other);
            Container.AutoSize();
        }

        private void SetOption(string group, object value)
        {
            switch (group)
            {
                case "Height":
                    Options.Height = (EODHeight)value;
                    break;
                case "Length":
                    Options.Length = (EODLength)value;
                    break;
                case "Buttons":
                    Options.Buttons = (byte)value;
                    break;
                case "Expandable":
                    Options.Expandable = (bool)value;
                    break;
                case "Timer":
                    Options.Timer = (EODTimer)value;
                    break;
                case "Tips":
                    Options.Tips = (EODTextTips)value;
                    break;
            }
            SetMode();
        }

        private void AddCheckBox(UIContainer parent, string caption, string groupName)
        {
            var formField = new UIHBoxContainer();
            parent.Add(formField);

            var check = new UIButton(GetTexture(0x0000083600000001));
            check.Tooltip = caption;
            check.OnButtonClick += x => {
                check.Selected = !check.Selected;
                SetOption(groupName, check.Selected);
            };

            formField.Add(check);

            formField.Add(new UILabel{
                Caption = caption
            });
        }

        private void AddRadioGroup(string title, string groupName, string[] optionLabels, object[] optionValues)
        {
            var vbox = new UIVBoxContainer();
            vbox.Add(new UILabel(){
                Caption = title
            });

            for (var i=0; i < optionLabels.Length; i++)
            {
                var formField = new UIHBoxContainer();
                vbox.Add(formField);

                var radio = new UIRadioButton();
                radio.RadioData = optionValues[i];
                radio.RadioGroup = groupName;
                radio.OnButtonClick += Radio_OnButtonClick;
                radio.Tooltip = optionLabels[i];

                formField.Add(radio);

                formField.Add(new UILabel {
                    Caption = optionLabels[i]
                });
            }

            Container.Add(vbox);
        }

        private void Radio_OnButtonClick(Framework.UIElement button)
        {
            var radio = (UIRadioButton)button;
            SetOption(radio.RadioGroup, radio.RadioData);
        }

        private void SetMode()
        {
            EODController.ShowEODMode(new EODLiveModeOpt {
                Buttons = Options.Buttons,
                Expandable = Options.Expandable,
                Height = Options.Height,
                Length = Options.Length,
                Timer = Options.Timer,
                Tips = Options.Tips
            });
        }
    }
}
