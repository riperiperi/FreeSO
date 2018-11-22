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
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;

namespace FSO.Client.UI.Controls
{
    public class UIListBox : UIElement
    {
        private UIMouseEventRef MouseHandler;
        public event ChangeDelegate OnChange;
        public event ButtonClickDelegate OnDoubleClick;

        public int NumVisibleRows { get; set; }
        public int ScrollOffset { get; set; }

        public bool AllowDisabledSelection = false;
        public bool Mask = false;

        public UIListBox()
        {
            MouseHandler = this.ListenForMouse(new Rectangle(0, 0, 10, 10), OnMouseEvent);
            RowHeight = 16;
        }




        #region Fields

        private int _RowHeight;

        [UIAttribute("rowHeight")]
        public int RowHeight
        {
            get { return _RowHeight; }
            set
            {
                _RowHeight = value;
                CalculateHitArea();
            }
        }

        private int m_VisibleRows = -1;

        [UIAttribute("visibleRows")]
        public int VisibleRows
        {
            get
            {
                return m_VisibleRows;
            }
            set
            {
                m_VisibleRows = value;
                CalculateScroll();
                CalculateHitArea();
            }
        }

        private TextStyle m_FontStyle;

        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle FontStyle
        {
            get
            {
                return m_FontStyle;
            }
            set
            {
                m_FontStyle = value;

                TextStyle = new UIListBoxTextStyle
                {
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
                m_SelectionTexture = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
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
                var previousSelection = SelectedItem;
                m_Items = value;
                SelectedItem = previousSelection;
                CalculateScroll();
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
        public override Vector2 Size
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

        #region Scrollbar

        private bool m_MouseOver = false;

        private int m_SelectedRow = -1;
        private int m_HoverRow = -1;

        [UIAttribute("scrollbarImage")]
        public Texture2D ScrollbarImage { get; set; }

        [UIAttribute("scrollbarGutter")]
        public int ScrollbarGutter { get; set; }

        private UISlider m_Slider;

        public UISlider Slider
        {
            get { return m_Slider; }
        }

        public void AttachSlider(UISlider slider)
        {
            this.m_Slider = slider;
            m_Slider.OnChange += new ChangeDelegate(m_Slider_OnChange);
            CalculateScroll();
        }

        public void InitDefaultSlider()
        {
            m_Slider = new UISlider();
            m_Slider.Texture = ScrollbarImage;
            AttachSlider(m_Slider);
            PositionChildSlider();
            Parent.Add(m_Slider);
        }

        public void PositionChildSlider()
        {
            m_Slider.Position = this.Position + new Vector2(this.Width + ScrollbarGutter, 0);
            m_Slider.SetSize(1, this.Height);
        }

        private void CalculateScroll()
        {
            var bounds = this.GetBounds();
            NumVisibleRows = m_VisibleRows != -1 ? m_VisibleRows : (int)Math.Floor((double)bounds.Height / (double)RowHeight);
            CalculateHitArea();
            var numRows = this.Items.Count;

            if (m_Slider != null)
            {
                m_Slider.MaxValue = (int)Math.Max(0, numRows - NumVisibleRows);
                m_Slider.MinValue = 0;
                m_Slider.Value = (float)ScrollOffset;
            }
        }

        void m_Slider_OnChange(UIElement element)
        {
            ScrollOffset = (int)m_Slider.Value;
        }

        #endregion

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var i = 0;
            foreach (var item in Items)
            {
                foreach (var col in item.Columns)
                {
                    if (col is UIElement)
                    {
                        var container = ((UIElement)col);
                        container.Visible = i >= ScrollOffset && i < ScrollOffset + NumVisibleRows;
                        if (container.Visible)
                        {
                            container.Parent = this.Parent;
                            container.InvalidateMatrix();
                            container.Update(state);
                            container.Parent = null;
                        } else
                        {
                            container.Update(state);
                        }
                    }
                }
                i++;
            }
            if (m_MouseOver)
            {
                var overRow = GetRowUnderMouse(state);
                m_HoverRow = overRow;
            }
            else
            {
                m_HoverRow = -1;
            }
        }

        private DoubleClick DoubleClicker = new DoubleClick();

        private void OnMouseEvent(UIMouseEventType type, UpdateState update)
        {
            if(OnDoubleClick != null && DoubleClicker.TryDoubleClick(type, update))
            {
                OnDoubleClick(null);
                return;
            }

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
                if (Items != null && m_SelectedRow >= 0 && m_SelectedRow < Items.Count)
                {
                    return Items[m_SelectedRow];
                }
                return null;
            }
            set
            {
                var index = Items.IndexOf(value);
                SelectedIndex = index;
            }
        }

        private void InternalSelect(int index)
        {
            Invalidate();
            m_SelectedRow = index;

            if (OnChange != null)
            {
                OnChange(this);
            }
        }

        private int GetRowUnderMouse(UpdateState update)
        {
            var mouse = this.GetMousePosition(update.MouseState);
            var estRow = (int)Math.Floor(mouse.Y / RowHeight) + ScrollOffset;
            if (estRow >= 0 && estRow < Items.Count)
            {
                /** Is this row enabled? **/
                var row = Items[estRow];
                if (ValuePointer.Get<Boolean>(row.Disabled) && AllowDisabledSelection == false) { return -1; }

                return estRow;
            }
            return -1;
        }

        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;

            CalculateHitArea();
            CalculateScroll();
        }

        private void CalculateHitArea()
        {
            MouseHandler.Region.Width = (int)m_Width;

            if (Mask)
            {
                MouseHandler.Region.Height = (int)m_Height;
            }
            else
            {
                MouseHandler.Region.Height = RowHeight * NumVisibleRows;
            }
        }

        private RenderTarget2D Target;

        public override void PreDraw(UISpriteBatch batch)
        {
            if (!Visible) return;
            base.PreDraw(batch);

            if (Mask)
            {
                var gd = batch.GraphicsDevice;
                var size = Size;
                if (Target == null || (int)size.X != Target.Width || (int)size.Y != Target.Height)
                {
                    Target?.Dispose();
                    Target = new RenderTarget2D(gd, (int)size.X, (int)size.Y, false, SurfaceFormat.Color, DepthFormat.None);
                }
                try { batch.End(); } catch { }
                gd.SetRenderTarget(Target);
                gd.Clear(Color.Transparent);
                var pos = LocalPoint(0, 0);

                var trans = Microsoft.Xna.Framework.Matrix.CreateTranslation(-pos.X, -pos.Y, 0);
                batch.BatchMatrixStack.Push(trans);
                batch.Begin(transformMatrix: trans, blendState: BlendState.AlphaBlend, sortMode: SpriteSortMode.Deferred);
                batch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                _Draw(batch);
                batch.End();
                batch.BatchMatrixStack.Pop();
                gd.SetRenderTarget(null);
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;

            //Mask
            if (Mask)
            {

                if (Target != null)
                {
                    DrawLocalTexture(batch, Target, Vector2.Zero);
                }
            }
            else
            {
                _Draw(batch);
            }         
        }

        private void _Draw(UISpriteBatch batch)
        {
            for (var i = 0; i < NumVisibleRows; i++)
            {
                var rowIndex = i + ScrollOffset;
                if ((rowIndex >= m_Items.Count) || rowIndex < 0)
                {
                    /** Out of bounds **/
                    continue;
                }

                var row = m_Items[rowIndex];
                var rowY = i * RowHeight;
                var columnX = 0;

                var selected = rowIndex == m_SelectedRow;
                var hover = rowIndex == m_HoverRow;
                if (selected)
                {
                    /** Draw selection background **/
                    var white = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
                    DrawLocalTexture(batch, white, null, new Vector2(0, rowY), new Vector2(m_Width, RowHeight), m_SelectionFillColor);
                }

                var ts = TextStyle;
                if (row.CustomStyle != null)
                {
                    ts = row.CustomStyle;
                }
                TextStyle style = null;
                var isDisabled = ValuePointer.Get<Boolean>(row.Disabled);

                if (ts != null)
                {
                    style = ts.Normal;
                    if (ValuePointer.Get<Boolean>(row.UseDisabledStyleByDefault))
                    {
                        style = ts.Disabled;
                    }
                    if (selected)
                    {
                        style = ts.Selected;
                    }
                    else if (hover)
                    {
                        style = ts.Highlighted;
                    }
                    else if (isDisabled)
                    {
                        style = ts.Disabled;
                    }
                }

                for (var x = 0; x < row.Columns.Length; x++)
                {
                    var columnValue = row.Columns[x];
                    var columnSpec = m_Columns[x];
                    var columnBounds = new Rectangle(0, 0, columnSpec.Width, RowHeight);

                    if(columnValue is FSO.Content.Model.ITextureRef){
                        columnValue = ((FSO.Content.Model.ITextureRef)columnValue).Get(batch.GraphicsDevice);
                    }

                    if (columnValue is string)
                    {
                        DrawLocalString(batch, style.TruncateToWidth((string)columnValue, columnSpec.Width), new Vector2(columnX, rowY), style, columnBounds, columnSpec.Alignment);
                    }
                    else if (columnValue is Texture2D)
                    {
                        var tex = (Texture2D)columnValue;
                        var texWidthDiv4 = tex.Width / 4;
                        /** We assume its a 4 state button **/
                        Rectangle from = new Rectangle(texWidthDiv4 * columnSpec.TextureDefaultFrame, 0, texWidthDiv4, tex.Height);
                        if (selected)
                        {
                            from.X = texWidthDiv4 * columnSpec.TextureSelectedFrame;
                        }
                        else if (hover)
                        {
                            from.X = texWidthDiv4 * columnSpec.TextureHoverFrame;
                        }
                        else if (isDisabled)
                        {
                            from.X = texWidthDiv4 * columnSpec.TextureDisabledFrame;
                        }

                        var destWidth = texWidthDiv4;
                        var destHeight = tex.Height;


                        if(columnSpec.TextureBounds != null && columnSpec.TextureBounds.HasValue)
                        {
                            var boundsX = columnSpec.TextureBounds.Value.X;
                            var boundsY = columnSpec.TextureBounds.Value.Y;

                            if (!columnSpec.TextureMaintainAspectRatio)
                            {
                                destWidth = (int)boundsX;
                                destHeight = (int)boundsY;
                            }
                            else
                            {
                                if(destWidth > destHeight)
                                {
                                    destWidth = (int)boundsX;
                                    destHeight = (int)(((float)tex.Height / (float)texWidthDiv4) * destWidth);
                                }
                                else
                                {
                                    destHeight = (int)boundsY;
                                    destWidth = (int)(((float)texWidthDiv4 / (float)tex.Height) * destHeight);
                                }
                            }
                        }

                        var to = new Vector2(columnX, rowY);
                        if ((columnSpec.Alignment & TextAlignment.Middle) == TextAlignment.Middle)
                        {
                            to.Y = rowY + ((RowHeight - destHeight) / 2);
                        }
                        else if ((columnSpec.Alignment & TextAlignment.Bottom) == TextAlignment.Bottom)
                        {
                            to.Y = rowY + ((RowHeight - destHeight));
                        }

                        if ((columnSpec.Alignment & TextAlignment.Center) == TextAlignment.Center)
                        {
                            to.X = columnX + ((columnBounds.Width - destWidth) / 2);
                        }
                        else if ((columnSpec.Alignment & TextAlignment.Right) == TextAlignment.Right)
                        {
                            to.X = columnX + (columnBounds.Width - destWidth);
                        }

                        DrawLocalTexture(batch, (Texture2D)columnValue, from, to, new Vector2((float)destWidth / (float)texWidthDiv4, (float)destHeight / (float)tex.Height));
                    }
                    else if (columnValue is UIElement)
                    {
                        var container = (UIElement)columnValue;

                        var to = new Vector2(columnX, rowY);
                        var bounds = container.GetBounds();
                        if ((columnSpec.Alignment & TextAlignment.Middle) == TextAlignment.Middle)
                        {
                            to.Y = rowY + ((RowHeight - bounds.Height) / 2);
                        }
                        else if ((columnSpec.Alignment & TextAlignment.Bottom) == TextAlignment.Bottom)
                        {
                            to.Y = rowY + ((RowHeight - bounds.Height));
                        }

                        if ((columnSpec.Alignment & TextAlignment.Center) == TextAlignment.Center)
                        {
                            to.X = columnX + ((columnBounds.Width - bounds.Width) / 2);
                        }
                        else if ((columnSpec.Alignment & TextAlignment.Right) == TextAlignment.Right)
                        {
                            to.X = columnX + (columnBounds.Width - bounds.Width);
                        }

                        container.Position = this.Position + to;
                        container.Parent = this.Parent;
                        container.InvalidationParent = this.InvalidationParent;
                        container.InvalidateMatrix();
                        container.PreDraw(batch);
                        container.Draw(batch);
                        container.Parent = null;
                    }
                    else if (columnValue != null)
                    {
                        //Convert it to a string
                        DrawLocalString(batch, (string)columnValue.ToString(), new Vector2(columnX, rowY), style, columnBounds, columnSpec.Alignment);
                    }

                    columnX += columnSpec.Width;
                }
            }
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, (int)m_Width, (int)m_Height);
        }

        public override void Removed()
        {
            Target?.Dispose();
            Target = null;
            base.Removed();
        }

    }

    public class UIListBoxColumn
    {
        public int Width = 50;
        public TextAlignment Alignment = TextAlignment.Left;

        public Vector2? TextureBounds;
        public bool TextureMaintainAspectRatio = true;

        public int TextureDefaultFrame = 0;
        public int TextureHoverFrame = 1;
        public int TextureSelectedFrame = 2;
        public int TextureDisabledFrame = 3;
    }

    public class UIListBoxColumnCollection : List<UIListBoxColumn>, UIAttributeParser
    {

        #region UIAttributeParser Members

        public void ParseAttribute(UINode node)
        {
            var columns = node["columns"].Split(new char[] { '|' });
            var alignments = new string[columns.Length];
            for (var i = 0; i < alignments.Length; i++)
            {
                alignments[i] = "1";
            }

            if (node.Attributes.ContainsKey("alignments"))
            {
                alignments = node.Attributes["alignments"].Split(new char[] { '|' });
            }

            for (var i = 0; i < columns.Length; i++)
            {
                var align = TextAlignment.Left;
                switch (alignments[i])
                {
                    case "2":
                        align = TextAlignment.Center | TextAlignment.Middle;
                        break;
                }

                this.Add(new UIListBoxColumn
                {
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


    public class UIListBoxItem
    {

        public object Data;
        public object[] Columns;
        public object Disabled = false;
        public UIListBoxTextStyle CustomStyle;
        public object UseDisabledStyleByDefault = false; //Offline avatars and properties use the disabled style without the row being disabled

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
