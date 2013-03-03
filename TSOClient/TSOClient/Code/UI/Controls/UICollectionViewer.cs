using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Specific variant of the grid view, includes a pagination bar
    /// </summary>
    public class UICollectionViewer : UIGridViewer
    {
        private UIPaginationBar m_Pagination;

        public UICollectionViewer()
            : base()
        {
            m_Pagination = new UIPaginationBar();
            m_Pagination.OnPageChanged += new PaginationEvent(m_Pagination_OnPageChanged);
            this.Add(m_Pagination);
        }

        void m_Pagination_OnPageChanged(UIElement from, int pageIndex)
        {
            this.SelectedPage = pageIndex;
        }

        public override void Init()
        {
            base.Init();

            m_Pagination.SetSize((int)Size.X, 45);
            m_Pagination.Y = Size.Y - 45;
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
                m_Pagination.TotalPages = (int)Math.Ceiling((double)value.Count / (double)ItemsPerPage);
            }
        }


        public override Vector2 GetGridArea()
        {
            /** Remove 45 px for the pagination control **/
            return new Vector2(base.Size.X, base.Size.Y - 45);
        }
    }



    public class UIGridViewerItem
    {
        public Texture2D Thumb;
        public object Data;
    }

}
