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
using TSOClient.Code.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Controls
{
    public class UIListBox : UIElement
    {
        [UIAttribute("rowHeight")]
        public int RowHeight { get; set; }
        private UIMouseEventRef MouseHandler;



        public event ChangeDelegate OnChange;

        public UIListBox()
        {
            RowHeight = 18;
            MouseHandler = this.ListenForMouse(new Rectangle(0, 0, 10, 10), OnMouseEvent);
        }




#region Fields

        private TextStyle m_FontStyle;

        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle FontStyle {
            get
            {
                return m_FontStyle;
            }
            set
            {
                m_FontStyle = value;

                TextStyle = new UIListBoxTextStyle {
                    Normal = value,
                    Highlighted = value,
                    Selected = value,
                    Disabled = value
                };
            }
        }

        public UIListBoxTextStyle TextStyle;


        [UIAttribute("selectionFillColor")]
        public Color SelectionFillColor
        {
            get
            {
                return m_SelectionFillColor;
            }
            set
            {
                m_SelectionFillColor = value;
                m_SelectionTexture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, value);
            }
        }

        private Color m_SelectionFillColor;
        private Texture2D m_SelectionTexture;


        private UIListBoxColumnCollection m_Columns = new UIListBoxColumnCollection();

        [UIAttribute("columns", typeof(UIListBoxColumnCollection))]
        public UIListBoxColumnCollection Columns
        {
            get { return m_Columns; }
            set
            {
                m_Columns = value;
            }
        }


        /// <summary>
        /// Set the content of the list
        /// </summary>
        private List<UIListBoxItem> m_Items = new List<UIListBoxItem>();
        public List<UIListBoxItem> Items
        {
            get
            {
                return m_Items;
            }
            set
            {
                m_Items = value;
            }
        }




        private float m_Width;
        private float m_Height;

        /// <summary>
        /// Component width
        /// </summary>
        public float Width
        {
            get { return m_Height; }
        }

        /// <summary>
        /// Component height
        /// </summary>
        public float Height
        {
            get { return m_Height; }
        }

        [UIAttribute("size")]
        public Vector2 Size
        {
            get
            {
                return new Vector2(m_Width, m_Height);
            }
            set
            {
                SetSize(value.X, value.Y);
            }
        }



#endregion


        private bool m_MouseOver = false;

        private int m_SelectedRow = -1;
        private int m_HoverRow = -1;

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (m_MouseOver)
            {
                var overRow = GetRowUnderMouse(state);
                m_HoverRow = overRow;
            }
        }

        private void OnMouseEvent(UIMouseEventType type, UpdateState update)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    m_MouseOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    m_MouseOver = false;
                    break;

                case UIMouseEventType.MouseUp:
                    /** Click **/
                    var row = GetRowUnderMouse(update);
                    if (row != -1)
                    {
                        /** Cant deselect once selected **/
                        InternalSelect(row);
                    }
                    break;
            }
        }

        public int SelectedIndex
        {
            get { return m_SelectedRow; }
            set
            {
                InternalSelect(value);
            }
        }

        public UIListBoxItem SelectedItem
        {
            get
            {
                if (m_SelectedRow >= 0 && m_SelectedRow < Items.Count)
                {
                    return Items[m_SelectedRow];
                }
                return null;
            }
        }

        private void InternalSelect(int index)
        {
            m_SelectedRow = index;

            if (OnChange != null)
            {
                OnChange(this);
            }
        }

        private int GetRowUnderMouse(UpdateState update)
        {
            var mouse = this.GetMousePosition(update.MouseState);
            var estRow = (int)Math.Floor(mouse.Y / RowHeight);
            if (estRow >= 0 && estRow < Items.Count)
            {
                /** Is this row enabled? **/
                var row = Items[estRow];
                if (row.Disabled) { return -1; }

                return estRow;
            }
            return -1;
        }


        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;

            MouseHandler.Region.Width = (int)width;
            MouseHandler.Region.Height = (int)height;
        }



        public override void Draw(UISpriteBatch batch)
        {
            for (var i = 0; i < m_Items.Count; i++)
            {
                var row = m_Items[i];
                var rowY = i * RowHeight;
                var columnX = 0;

                var selected = i == m_SelectedRow;
                var hover = i == m_HoverRow;
                if (selected)
                {
                    /** Draw selection background **/
                    DrawLocalTexture(batch, m_SelectionTexture, null, new Vector2(0, rowY), new Vector2(m_Width, RowHeight));
                }

                var ts = TextStyle;
                if (row.CustomStyle != null)
                {
                    ts = row.CustomStyle;
                }
                var style = ts.Normal;
                if (selected)
                {
                    style = ts.Selected;
                }
                else if (hover)
                {
                    style = ts.Highlighted;
                }
                else if (row.Disabled)
                {
                    style = ts.Disabled;
                }


                for (var x = 0; x < row.Columns.Length; x++)
                {
                    var columnValue = row.Columns[x];
                    var columnSpec = m_Columns[x];
                    var columnBounds = new Rectangle(0, 0, columnSpec.Width, RowHeight);

                    if (columnValue is string)
                    {
                        DrawLocalString(batch, (string)columnValue, new Vector2(columnX, rowY), style, columnBounds, columnSpec.Alignment);
                    }

                    columnX += columnSpec.Width;
                }
            }
        }


        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, (int)m_Width, (int)m_Height);
        }

    }


    public class UIListBoxColumn
    {
        public int Width = 50;
        public TextAlignment Alignment = TextAlignment.Left;
    }

    public class UIListBoxColumnCollection : List<UIListBoxColumn>, UIAttributeParser
    {

        #region UIAttributeParser Members

        public void ParseAttribute(UINode node)
        {
            var columns = node["columns"].Split(new char[] { '|' });
            var alignments = new string[columns.Length];
            for (var i = 0; i < alignments.Length; i++){
                alignments[i] = "1";
            }

            if (node.Attributes.ContainsKey("alignments")){
                alignments = node.Attributes["alignments"].Split(new char[] { '|' });
            }

            for (var i = 0; i < columns.Length; i++)
            {
                var align = TextAlignment.Left;
                switch (alignments[i])
                {
                    case "2":
                        align = TextAlignment.Center;
                        break;
                }

                this.Add(new UIListBoxColumn {
                    Width = int.Parse(columns[i]),
                    Alignment = align
                });
            }

            /*
             * if (node.Attributes.ContainsKey("font"))
            {
                fontSize = int.Parse(node.Attributes["font"]);
            }*/
        }

        #endregion
    }


    public class UIListBoxItem {

        public object Data;
        public object[] Columns;
        public bool Disabled = false;
        public UIListBoxTextStyle CustomStyle;

        public UIListBoxItem(object data, params object[] columns)
        {
            this.Data = data;
            this.Columns = columns;
        }
    }


    public class UIListBoxTextStyle
    {
        public TextStyle Normal;
        public TextStyle Selected;
        public TextStyle Highlighted;
        public TextStyle Disabled;

        public UIListBoxTextStyle(TextStyle baseStyle)
        {
            Normal = Selected = Highlighted = Disabled = baseStyle;
        }

        public UIListBoxTextStyle()
        {
        }

        [UIAttribute("normal")]
        public Color NormalColor
        {
            set
            {
                Normal = Normal.Clone();
                Normal.Color = value;
            }
        }

        [UIAttribute("selected")]
        public Color SelectedColor
        {
            set
            {
                Selected = Selected.Clone();
                Selected.Color = value;
            }
        }

        [UIAttribute("highlighted")]
        public Color HighlightedColor
        {
            set
            {
                Highlighted = Highlighted.Clone();
                Highlighted.Color = value;
            }
        }

        [UIAttribute("disabled")]
        public Color DisabledColor
        {
            set
            {
                Disabled = Disabled.Clone();
                Disabled.Color = value;
            }
        }

    }

}
