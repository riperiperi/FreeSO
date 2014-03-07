/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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

        private UIAlertResult m_Result = new UIAlertResult();

        /// <summary>
        /// Which button was clicked?
        /// </summary>
        public UIAlertResult ClickResult
        {
            get { return m_Result; }
        }

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
            if ((options.Buttons & UIAlertButtons.OK) == UIAlertButtons.OK)
            {
                buttons.Add(AddButton(GameFacade.Strings.GetString("142", "ok button"), UIAlertButtons.OK));
            }

            /** Position buttons **/
            var btnX = (w - ((buttons.Count * 100) + ((buttons.Count - 1) * 45))) / 2;
            var btnY = h - 58;
            foreach (var button in buttons)
            {
                button.Y = btnY;
                button.X = btnX;
                btnX += 45;
            }
        }

        public void CenterAround(UIElement element)
        {
            CenterAround(element, 0, 0);
        }

        public void CenterAround(UIElement element, int offsetX, int offsetY)
        {
            var bounds = element.GetBounds();
            if (bounds == null) { return; }

            var topLeft = 
                element.LocalPoint(new Microsoft.Xna.Framework.Vector2(bounds.X, bounds.Y));

            topLeft = GameFacade.Screens.CurrentUIScreen.GlobalPoint(topLeft);


            this.X = offsetX + topLeft.X + ((bounds.Width - this.Width) / 2);
            this.Y = offsetY + topLeft.Y + ((bounds.Height - this.Height) / 2);
        }

        private Dictionary<UIElement, UIAlertButtons> ButtonMap = new Dictionary<UIElement, UIAlertButtons>();
        private UIButton AddButton(string label, UIAlertButtons type)
        {
            var btn = new UIButton();
            btn.Caption = label;
            btn.Width = 100;
            btn.OnButtonClick += new ButtonClickDelegate(btn_OnButtonClick);

            ButtonMap.Add(btn, type);

            this.Add(btn);
            return btn;
        }

        private void btn_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
            m_Result.Button = ButtonMap[button];
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
        Cancel
    }

    public class UIAlertResult
    {
        public UIAlertButtons Button;
    }
}
