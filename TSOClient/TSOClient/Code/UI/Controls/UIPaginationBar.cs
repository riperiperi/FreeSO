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

        private int myPageStartIdx;
        private UILabel myCountLabel; 
        private UIButton myLeftButton;
        private UIButton myRightButton;
        private UIClickableLabel[] myTextButtons = new UIClickableLabel[0];
        private UIContainer myNumCtnr;

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


        private int m_SelectedPage;
        public int SelectedPage
        {
            get
            {
                return m_SelectedPage;
            }
            set
            {
                m_SelectedPage = value;
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
            }

            var numsToShow = Math.Min(m_TotalPages, (int)Math.Floor((double)m_Width / 15));

            myTextButtons = new UIClickableLabel[numsToShow];
            var stride = 0;
            for (int i = 0; i < numsToShow; i++)
            {
                var btn = new UIClickableLabel()
                {
                    X = i * 15,
                    Caption = (i + 1).ToString(),
                    Size = new Microsoft.Xna.Framework.Vector2(15, 15)
                };
                myTextButtons[i] = btn;
                myTextButtons[i].OnButtonClick += new ButtonClickDelegate(UIPaginationBar_OnButtonClick);
                myNumCtnr.Add(myTextButtons[i]);
            }

            SetSize(m_Width, m_Height);
        }

        private void InternalSetPage(int page)
        {
            m_SelectedPage = page;
            if (OnPageChanged != null)
            {
                OnPageChanged(this, page);
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

            double barWidth = myTextButtons.Length * 15;
            myNumCtnr.X = (float) (width - barWidth) / 2;
        }

    }
}
