/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.LUI;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// UIAlert is a messagebox that can be displayed to the user with several different buttons.
    /// </summary>
    public class UIAlert : UIDialog
    {
        private UIAlertOptions m_Options;
        private TextRendererResult m_MessageText;
        private TextStyle m_TextStyle;

        private UIImage Icon;
        private Vector2 IconSpace;

        private List<UIButton> Buttons;

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

            /** Add buttons **/
            Buttons = new List<UIButton>();
            if (options.Buttons == UIAlertButtons.OK)
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "ok button"), UIAlertButtons.OK, true));
            else if (options.Buttons == UIAlertButtons.OKCancel)
            {
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "ok button"), UIAlertButtons.OK, false));
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "cancel button"), UIAlertButtons.Cancel, true));
            }
            else if (options.Buttons == UIAlertButtons.Yes)
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "yes button"), UIAlertButtons.Yes, true));
            else if(options.Buttons == UIAlertButtons.No)
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "no button"), UIAlertButtons.No, true));
            else if(options.Buttons == UIAlertButtons.YesNo)
            {
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "yes button"), UIAlertButtons.Yes, false));
                Buttons.Add(AddButton(GameFacade.Strings.GetString("142", "no button"), UIAlertButtons.No, true));
            }

            /** Position buttons **/
            RefreshSize();
        }

        public void RefreshSize()
        {
            var w = m_Options.Width;
            var h = m_Options.Height;
            h = Math.Max(h, Math.Max((int)IconSpace.Y, m_MessageText.BoundingBox.Height) + 74);

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
        public Dictionary<UIAlertButtons, UIButton> ButtonMap = new Dictionary<UIAlertButtons, UIButton>();

        /// <summary>
        /// Adds a button to this message box.
        /// </summary>
        /// <param name="label">Label of the button.</param>
        /// <param name="type">Type of the button to be added.</param>
        /// <param name="InternalHandler">Should the button's click be handled internally?</param>
        /// <returns></returns>
        private UIButton AddButton(string label, UIAlertButtons type, bool InternalHandler)
        {
            var btn = new UIButton();
            btn.Caption = label;
            btn.Width = 100;

            if(InternalHandler)
                btn.OnButtonClick += new ButtonClickDelegate(btn_OnButtonClick);

            ButtonMap.Add(type, btn);

            this.Add(btn);
            return btn;
        }

        private void btn_OnButtonClick(UIElement button)
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

        public UIAlertButtons Buttons = UIAlertButtons.OK;
    }

    [Flags]
    public enum UIAlertButtons
    {
        OK,
        Cancel,
        OKCancel,
        Yes,
        No,
        YesNo
    }

    public class UIAlertResult
    {
        public UIAlertButtons Button;
    }
}
