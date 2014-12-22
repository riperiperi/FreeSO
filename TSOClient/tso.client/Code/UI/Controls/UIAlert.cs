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

        public UIAlert(UIAlertOptions options) : base(UIDialogStyle.Standard, true)
        {
            this.m_Options = options;
            this.Caption = options.Title;
            this.Opacity = 0.9f;

            m_TextStyle = TextStyle.DefaultLabel.Clone();
            m_TextStyle.Size = 10;

            /** Determine the size **/
            ComputeText();

            //32 from either edge
            var w = options.Width;
            var h = options.Height;
            h = Math.Max(h, m_MessageText.BoundingBox.Height + 74);

            SetSize(w, h);

            /** Add buttons **/
            var buttons = new List<UIButton>();
            if (options.Buttons == UIAlertButtons.OK)
            {
                buttons.Add(AddButton(GameFacade.Strings.GetString("142", "ok button"), UIAlertButtons.OK, true));
            }
            else if (options.Buttons == UIAlertButtons.OKCancel)
            {
                buttons.Add(AddButton(GameFacade.Strings.GetString("142", "ok button"), UIAlertButtons.OK, false));
                buttons.Add(AddButton(GameFacade.Strings.GetString("142", "cancel button"), UIAlertButtons.Cancel, true));
            }

            /** Position buttons **/
            var btnX = (w - ((buttons.Count * 100) + ((buttons.Count - 1) * 45))) / 2;
            var btnY = h - 58;
            foreach (UIElement button in buttons)
            {
                button.Y = btnY;
                button.X = btnX;
                btnX += 150;
            }
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
                Alignment = TextAlignment.Center,
                MaxWidth = m_Options.Width - 64,
                Position = new Microsoft.Xna.Framework.Vector2(32, 38),
                Scale = _Scale,
                TextStyle = m_TextStyle,
                WordWrap = true
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

        public UIAlertButtons Buttons = UIAlertButtons.OK;
    }

    [Flags]
    public enum UIAlertButtons
    {
        OK,
        Cancel,
        OKCancel
    }

    public class UIAlertResult
    {
        public UIAlertButtons Button;
    }
}
