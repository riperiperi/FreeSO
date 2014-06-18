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
    public delegate void PaginationEvent(UIElement from, int pageIndex);

    /// <summary>
    /// Pagination bar used by the UICollectionViewer to navigate head and body skins
    /// </summary>
    public class UIPaginationBar : UIContainer
    {
        public event PaginationEvent OnPageChanged;

        private UIClickableLabel[] myTextButtons = new UIClickableLabel[0];
        private UIContainer myNumCtnr;

        public int ItemSize = 16;

        public TextStyle TextStyle = TextStyle.DefaultLabel;
        public TextStyle SelectedTextStyle = TextStyle.DefaultLabel;


        public UIPaginationBar()
        {
            myNumCtnr = new UIContainer();
            this.Add(myNumCtnr);
        }



        private int m_TotalPages;
        public int TotalPages
        {
            get
            {
                return m_TotalPages;
            }
            set
            {
                m_TotalPages = value;
                Redraw();
            }
        }


        private UIClickableLabel m_SelectedButton;
        private int m_SelectedPage = 0;
        public int SelectedPage
        {
            get
            {
                return m_SelectedPage;
            }
            set
            {
                if (m_SelectedButton != null)
                {
                    m_SelectedButton.CaptionStyle = TextStyle;
                }
                m_SelectedPage = value;
                m_SelectedPage = Math.Min(m_SelectedPage, m_TotalPages-1);
                m_SelectedPage = Math.Max(0, m_SelectedPage);


                myTextButtons[m_SelectedPage].CaptionStyle = SelectedTextStyle;
                m_SelectedButton = myTextButtons[m_SelectedPage];
            }
        }




        private void Redraw()
        {
            if (myTextButtons != null)
            {
                foreach (var btn in myTextButtons)
                {
                    myNumCtnr.Remove(btn);
                }
                m_SelectedButton = null;
            }

            var numsToShow = Math.Min(m_TotalPages, (int)Math.Floor((double)m_Width / 15));

            myTextButtons = new UIClickableLabel[numsToShow];
            for (int i = 0; i < numsToShow; i++)
            {
                var btn = new UIClickableLabel()
                {
                    X = i * ItemSize,
                    Caption = (i + 1).ToString(),
                    Size = new Microsoft.Xna.Framework.Vector2(ItemSize, ItemSize),
                    CaptionStyle = TextStyle
                };
                myTextButtons[i] = btn;
                myTextButtons[i].OnButtonClick += new ButtonClickDelegate(UIPaginationBar_OnButtonClick);
                myNumCtnr.Add(myTextButtons[i]);
            }

            SetSize(m_Width, m_Height);
            SelectedPage = m_SelectedPage;
        }

        private void InternalSetPage(int page)
        {
            SelectedPage = page;
            if (OnPageChanged != null)
            {
                OnPageChanged(this, m_SelectedPage);
            }
        }

        void UIPaginationBar_OnButtonClick(UIElement button)
        {
            /**
             * Called when a pagination item is clicked
             */
            var index = Array.IndexOf(myTextButtons, button);
            if (index != -1)
            {
                InternalSetPage(index);
            }
        }



        private int m_Width;
        private int m_Height;

        public void SetSize(int width, int height)
        {
            m_Width = width;
            m_Height = height;

            double barWidth = myTextButtons.Length * ItemSize;
            myNumCtnr.X = (float) (width - barWidth) / 2;
        }

    }
}
