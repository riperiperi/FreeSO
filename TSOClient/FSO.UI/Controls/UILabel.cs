/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.Utils;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// A drawable label containing text.
    /// </summary>
    public class UILabel : UIElement, IUIAutoSize
    {
        /// <summary>
        /// The font to use when rendering the label
        /// </summary>
        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle CaptionStyle { get; set; }
        private string m_Text = "";

        [UIAttribute("text", DataType=UIAttributeType.StringTable)]
        public string Caption
        {
            get { return m_Text; }
            set {
                if (value != m_Text) Invalidate();
                m_Text = value;
                if (_InDraw)
                {
                    int y = 22;
                }
                _WrappedOutput = null; }
        }

        
        public int NumLines
        {
            get
            {
                if (Wrapped)
                {
                    if (_WrappedOutput == null)
                    {
                        var scale = new Vector2(CaptionStyle.Scale);
                        _WrappedOutput = UIUtils.WordWrap(m_Text, m_Size.Width, CaptionStyle, scale);
                    }
                    return _WrappedOutput.Lines.Count;
                }
                return 1;
            }
        }

        /// <summary>
        /// If size is set you can make use of alignment settings
        /// </summary>
        [UIAttribute("size")]
        public override Vector2 Size
        {
            get
            {
                if (m_Size != null)
                {
                    return new Vector2(m_Size.Width, m_Size.Height);
                }
                return Vector2.Zero;
            }
            set
            {
                m_Size = new Rectangle(0, 0, (int)value.X, (int)value.Y);
                _WrappedOutput = null;
            }
        }
        private Rectangle m_Size;

        public UILabel()
        {
            CaptionStyle = TextStyle.DefaultLabel;
        }

        public TextAlignment Alignment = TextAlignment.Center;

        [UIAttribute("alignment")]
        public int _Alignment
        {
            set
            {
                switch (value)
                {
                    case 3:
                        Alignment = TextAlignment.Center | TextAlignment.Middle;
                        break;
                }
            }
        }

        private bool _Wrapped;
        [UIAttribute("wrapped")]
        public bool Wrapped
        {
            get { return _Wrapped; }
            set
            {
                _Wrapped = value;
                _WrappedOutput = null;
            }
        }

        private UIWordWrapOutput _WrappedOutput = null;
        private bool _InDraw = false;

        public override void Draw(UISpriteBatch SBatch)
        {
            _InDraw = true;

            if (!Visible)
            {
                _InDraw = false;
                return;
            }

            if (m_Text != null && CaptionStyle != null)
            {
                if (m_Size != Rectangle.Empty)
                {
                    if (_Wrapped)
                    {
                        if (_WrappedOutput == null)
                        {
                            var scale = new Vector2(CaptionStyle.Scale);
                            _WrappedOutput = UIUtils.WordWrap(m_Text, m_Size.Width, CaptionStyle, scale);
                        }

                        if(_WrappedOutput == null || _WrappedOutput.Lines == null){
                            _InDraw = false;
                            return;
                        }

                        var y = 0;
                        if((Alignment & TextAlignment.Middle) == TextAlignment.Middle)
                        {
                            y = (m_Size.Height - _WrappedOutput.Height) / 2;
                        }else if((Alignment & TextAlignment.Bottom) == TextAlignment.Bottom){
                            y = m_Size.Height - _WrappedOutput.Height;
                        }

                        for (int i=0; i < _WrappedOutput.Lines.Count; i++)
                        {
                            var line = _WrappedOutput.Lines[i];
                            var rect = new Rectangle(0, 0, m_Size.Width, CaptionStyle.LineHeight);
                            DrawLocalString(SBatch, line, new Vector2(0, y), CaptionStyle, rect, Alignment);
                            y += rect.Height;
                        }
                    }
                    else
                    {
                        DrawLocalString(SBatch, m_Text, Vector2.Zero, CaptionStyle, m_Size, Alignment);
                    }
                }
                else
                {
                    DrawLocalString(SBatch, m_Text, Vector2.Zero, CaptionStyle);
                }
            }

            _InDraw = false;
        }

        public void NewStyle(Color color, int size)
        {
            CaptionStyle = CaptionStyle.Clone();
            CaptionStyle.Color = color;
            CaptionStyle.Size = size;
        }

        public void AutoSize()
        {
            this.Size = CaptionStyle.MeasureString(Caption);
        }
    }
}
