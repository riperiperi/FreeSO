/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// UIAlert is a messagebox that can be displayed to the user with several different buttons.
    /// </summary>
    public class UIAlert : UIDialog
    {
        private UIAlertOptions m_Options;
        private TextRendererResult m_MessageText;
        private TextStyle m_TextStyle;
        
        private UIProgressBar _ProgressBar;

        private UIImage Icon;
        private Vector2 IconSpace;

        private List<UIButton> Buttons;
        private UITextBox TextBox;

        public string ResponseText
        {
            get
            {
                return (TextBox == null) ? null : TextBox.CurrentText;
            }
            set
            {
                if (TextBox != null) TextBox.CurrentText = value;
            }
        }

        public UIAlert(UIAlertOptions options) : base(UIDialogStyle.Standard, true)
        {
            this.m_Options = options;
            this.Caption = options.Title;
            this.Opacity = 0.9f;

            m_TextStyle = TextStyle.DefaultLabel.Clone();
            m_TextStyle.Size = options.TextSize;

            Icon = new UIImage();
            Icon.Position = new Vector2(32, 32);
            Icon.SetSize(0, 0);
            Add(Icon);

            /** Determine the size **/
            ComputeText();

            if (options.ProgressBar)
            {
                _ProgressBar = new UIProgressBar();
                _ProgressBar.Mode = ProgressBarMode.Animated;
                _ProgressBar.Position = new Microsoft.Xna.Framework.Vector2(32, 0);
                _ProgressBar.SetSize(options.Width - 64, 26);
                this.Add(_ProgressBar);
            }

            /** Add buttons **/
            Buttons = new List<UIButton>();

            foreach (var button in options.Buttons)
            {
                string buttonText = "";
                if (button.Text != null) buttonText = button.Text;
                else
                {
                    switch (button.Type)
                    {
                        case UIAlertButtonType.OK:
                            buttonText = GameFacade.Strings.GetString("142", "ok button");
                            break;
                        case UIAlertButtonType.Yes:
                            buttonText = GameFacade.Strings.GetString("142", "yes button");
                            break;
                        case UIAlertButtonType.No:
                            buttonText = GameFacade.Strings.GetString("142", "no button");
                            break;
                        case UIAlertButtonType.Cancel:
                            buttonText = GameFacade.Strings.GetString("142", "cancel button");
                            break;
                    }
                }
                var btnElem = AddButton(buttonText, button.Type, button.Handler == null);
                Buttons.Add(btnElem);
                if (button.Handler != null) btnElem.OnButtonClick += button.Handler;
            }

            if (options.TextEntry)
            {
                TextBox = new UITextBox();
                this.Add(TextBox);
            }

            /** Position buttons **/
            RefreshSize();
        }

        public void RefreshSize()
        {
            var w = m_Options.Width;
            var h = m_Options.Height;

            h = Math.Max(h, Math.Max((int)IconSpace.Y, m_MessageText == null ? 0 : m_MessageText.BoundingBox.Height) + 32);

            if(_ProgressBar != null){
                _ProgressBar.Position = new Vector2(_ProgressBar.Position.X, h + 12);
                h += 47;
            }

            if(Buttons.Count > 0)
            {
                h += 58;
            }
            else
            {
                h += 32;
            }

            if (m_Options.TextEntry)
            {
                TextBox.X = 32;
                TextBox.Y = h - 54;
                TextBox.SetSize(w - 64, 25);
                h += 45;
            }

            SetSize(w, h);

            var btnX = (w - ((Buttons.Count * 100) + ((Buttons.Count - 1) * 45))) / 2;
            var btnY = h - 58;
            foreach (UIElement button in Buttons)
            {
                button.Y = btnY;
                button.X = btnX;
                btnX += 150;
            }
        }

        public void SetIcon(Texture2D img, int width, int height)
        {
            Icon.Texture = img;
            IconSpace = new Vector2(width+15, height);

            float scale = Math.Min(1, Math.Min((float)height / (float)img.Height, (float)width / (float)img.Width));
            Icon.SetSize(img.Width * scale, img.Height * scale);
            Icon.Position = new Vector2(32, 38) + new Vector2(width/2 - (img.Width * scale / 2), height/2 - (img.Height * scale / 2));

            ComputeText();
            RefreshSize();
        }

        public new void CenterAround(UIElement element)
        {
            CenterAround(element, 0, 0);
        }

        public new void CenterAround(UIElement element, int offsetX, int offsetY)
        {
            var bounds = element.GetBounds();
            if (bounds == null) { return; }

            var topLeft = 
                element.LocalPoint(new Microsoft.Xna.Framework.Vector2(bounds.X, bounds.Y));

            topLeft = GameFacade.Screens.CurrentUIScreen.GlobalPoint(topLeft);


            this.X = offsetX + topLeft.X + ((bounds.Width - this.Width) / 2);
            this.Y = offsetY + topLeft.Y + ((bounds.Height - this.Height) / 2);
        }

        /// <summary>
        /// Map of buttons attached to this message box.
        /// </summary>
        public Dictionary<UIAlertButtonType, UIButton> ButtonMap = new Dictionary<UIAlertButtonType, UIButton>();

        /// <summary>
        /// Adds a button to this message box.
        /// </summary>
        /// <param name="label">Label of the button.</param>
        /// <param name="type">Type of the button to be added.</param>
        /// <param name="InternalHandler">Should the button's click be handled internally?</param>
        /// <returns></returns>
        private UIButton AddButton(string label, UIAlertButtonType type, bool InternalHandler)
        {
            var btn = new UIButton();
            btn.Caption = label;
            btn.Width = 100;

            if(InternalHandler)
                btn.OnButtonClick += new ButtonClickDelegate(x =>
                {
                    HandleClose();
                });

            ButtonMap.Add(type, btn);

            this.Add(btn);
            return btn;
        }
        
        private void HandleClose()
        {
            UIScreen.RemoveDialog(this);
        }

        private bool m_TextDirty = false;
        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();
            m_TextDirty = true;
        }

        private void ComputeText()
        {
            m_MessageText = TextRenderer.ComputeText(m_Options.Message, new TextRendererOptions
            {
                Alignment = m_Options.Alignment,
                MaxWidth = m_Options.Width - 64,
                Position = new Microsoft.Xna.Framework.Vector2(32, 38),
                Scale = _Scale,
                TextStyle = m_TextStyle,
                WordWrap = true,
                TopLeftIconSpace = IconSpace
            }, this);

            m_TextDirty = false;
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if (m_TextDirty)
            {
                ComputeText();
            }

            TextRenderer.DrawText(m_MessageText.DrawingCommands, this, batch);
        }
    }

    public class UIAlertOptions
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public int Width = 340;
        public int Height = -1;
        public TextAlignment Alignment = TextAlignment.Center;

        public int TextSize = 10;
        public bool ProgressBar;

        public bool TextEntry = false;
        public UIAlertButton[] Buttons = new UIAlertButton[] { new UIAlertButton() };
    }

    public class UIAlertButton
    {
        public static UIAlertButton[] Ok()
        {
            return Ok(null);
        }

        public static UIAlertButton[] Ok(ButtonClickDelegate callback)
        {
            return new UIAlertButton[] { new UIAlertButton(UIAlertButtonType.OK, callback) };
        }

        public static UIAlertButton[] YesNo(ButtonClickDelegate yesCallback, ButtonClickDelegate noCallback)
        {
            return new UIAlertButton[] { new UIAlertButton(UIAlertButtonType.Yes, yesCallback), new UIAlertButton(UIAlertButtonType.No, noCallback) };
        }

        public UIAlertButtonType Type = UIAlertButtonType.OK;
        public ButtonClickDelegate Handler = null; //if null, just use default (exit UIAlert)
        public string Text = null; //custom text, if null then we just use cst.

        public UIAlertButton() { }
        public UIAlertButton(UIAlertButtonType type) { Type = type; }
        public UIAlertButton(UIAlertButtonType type, ButtonClickDelegate handler) { Type = type; Handler = handler; }
        public UIAlertButton(UIAlertButtonType type, ButtonClickDelegate handler, string text) { Type = type; Handler = handler; Text = text; }
    }

    public enum UIAlertButtonType
    {
        None,
        OK,
        Yes,
        No,
        Cancel,
    }

    public class UIAlertResult
    {
        public UIAlertButtonType Button;
        public string Text;
    }
}
