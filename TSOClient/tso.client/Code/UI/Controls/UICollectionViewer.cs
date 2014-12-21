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
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSO.Common.utils;

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
            m_Pagination.TextStyle = TextStyle.DefaultLabel.Clone();
            m_Pagination.TextStyle.Color = Color.White;

            m_Pagination.SelectedTextStyle = TextStyle.DefaultLabel.Clone();
            m_Pagination.SelectedTextStyle.Size++;

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
        public Promise<Texture2D> Thumb;
        public object Data;
    }

}
