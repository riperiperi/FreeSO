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
using Microsoft.Xna.Framework;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using FSO.Common.Utils;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Specific variant of the grid view, includes a pagination bar
    /// </summary>
    public class UICollectionViewer : UIGridViewer
    {
        private UIPaginationBar m_PaginationBar;
        private UIPaginationStyle m_PaginationStyle = UIPaginationStyle.PAGINATION_BAR;
        private Texture2D m_RightArrow;
        private Texture2D m_LeftArrow;

        private UIButton LeftArrow;
        private UIButton RightArrow;

        public float PaginationHeight = 45;
        public float PaginationHeightDeduction = 45;

        public UICollectionViewer()
            : base()
        {
            this.OnSelectedPageChanged += UICollectionViewer_OnSelectedPageChanged;
        }

        [UIAttribute("rightArrowImage")]
        public Texture2D RightArrowImage
        {
            get { return m_RightArrow; }
            set
            {
                m_RightArrow = value;
            }
        }

        [UIAttribute("leftArrowImage")]
        public Texture2D LeftArrowImage
        {
            get { return m_LeftArrow; }
            set
            {
                m_LeftArrow = value;
            }
        }

        public UIPaginationStyle PaginationStyle
        {
            get { return m_PaginationStyle; }
            set
            {
                m_PaginationStyle = value;
            }
        }

        private void CreatePaginationUI()
        {
            if(m_PaginationStyle == UIPaginationStyle.PAGINATION_BAR)
            {
                //TODO: Arrows
                m_PaginationBar = new UIPaginationBar();
                m_PaginationBar.OnPageChanged += m_Pagination_OnPageChanged;
                m_PaginationBar.TextStyle = TextStyle.DefaultLabel.Clone();
                m_PaginationBar.TextStyle.Color = Color.White;

                m_PaginationBar.SelectedTextStyle = TextStyle.DefaultLabel.Clone();
                m_PaginationBar.SelectedTextStyle.Size++;

                this.Add(m_PaginationBar);
            }
            else
            {
                if (m_LeftArrow == null || m_RightArrow == null)
                {
                    throw new Exception("Arrow images must be provided");
                }

                LeftArrow = new UIButton(m_LeftArrow);
                LeftArrow.OnButtonClick += Arrow_OnButtonClick;
                Add(LeftArrow);
                RightArrow = new UIButton(m_RightArrow);
                RightArrow.OnButtonClick += Arrow_OnButtonClick;
                Add(RightArrow);
            }
        }

        private void Arrow_OnButtonClick(UIElement button)
        {
            if(button == LeftArrow){
                SelectedPage--;
            }else{
                SelectedPage++;
            }
        }

        public override void Init()
        {
            CreatePaginationUI();
            base.Init();
            PositionPagination();
        }

        private void PositionPagination()
        {
            if (m_PaginationStyle == UIPaginationStyle.PAGINATION_BAR)
            {
                m_PaginationBar.SetSize((int)Size.X, (int)PaginationHeight);
                m_PaginationBar.Y = Size.Y - PaginationHeight;
            }else if(m_PaginationStyle == UIPaginationStyle.LEFT_RIGHT_ARROWS)
            {
                LeftArrow.Position = new Vector2(0, (Size.Y - LeftArrow.Size.Y) / 2);
                RightArrow.Position = new Vector2(Size.X - RightArrow.Size.X, (Size.Y - RightArrow.Size.Y) / 2);
            }
        }



        private void UICollectionViewer_OnSelectedPageChanged(UIElement element)
        {
            UpdatePaginationState();
        }

        private void UpdatePaginationState()
        {
            if (DataProvider == null) { return; }

            var numPages = NumPages;

            if (m_PaginationStyle == UIPaginationStyle.PAGINATION_BAR){
                m_PaginationBar.TotalPages = numPages;
            }else if (m_PaginationStyle == UIPaginationStyle.LEFT_RIGHT_ARROWS){
                LeftArrow.Disabled = SelectedPage == 0;
                RightArrow.Disabled = SelectedPage == (numPages-1);
            }
        }

        void m_Pagination_OnPageChanged(UIElement from, int pageIndex)
        {
            this.SelectedPage = pageIndex;
        }


        public override List<object> DataProvider
        {
            get
            {
                return base.DataProvider;
            }
            set
            {
                base.DataProvider = value;
                UpdatePaginationState();
            }
        }


        public override Rectangle GetGridArea()
        {
            if (m_PaginationStyle == UIPaginationStyle.PAGINATION_BAR)
            {
                /** Remove 45 px for the pagination control **/
                return new Rectangle(0, 0, (int)base.Size.X, (int)(base.Size.Y - PaginationHeightDeduction));
            }else if(m_PaginationStyle == UIPaginationStyle.LEFT_RIGHT_ARROWS && LeftArrow != null && RightArrow != null)
            {
                return new Rectangle((int)LeftArrow.Size.X, 0, (int)(Size.X - (LeftArrow.Size.X + RightArrow.Size.X)), (int)Size.Y);
            }
            return base.GetGridArea();
        }
    }

    public enum UIPaginationStyle
    {
        LEFT_RIGHT_ARROWS,
        PAGINATION_BAR
    }

    public class UIGridViewerItem
    {
        public Promise<Texture2D> Thumb;
        public object Data;
    }

}
