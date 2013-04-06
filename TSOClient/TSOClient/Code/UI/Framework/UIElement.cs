/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;
using System.IO;
using TSOClient.Code.UI.Framework.Parser;
using System.Threading;

namespace TSOClient.Code.UI.Framework
{
    /// <summary>
    /// Base class for all UIElements.
    /// </summary>
    public abstract class UIElement
    {
        protected string m_StringID;

        /// <summary>
        /// X position of this UIComponent
        /// </summary>
        protected float _X;
        
        /// <summary>
        /// Y position of this UIComponent
        /// </summary>
        protected float _Y;

        /// <summary>
        /// Scale factor on the x axis
        /// </summary>
        protected float _ScaleX = 1.0f;

        /// <summary>
        /// Scale factor on the y axis
        /// </summary>
        protected float _ScaleY = 1.0f;

        /// <summary>
        /// Transparency value for this UIElement
        /// </summary>
        protected float _Opacity = 1.0f;
        protected Color _OpacityColor = Color.White;
        protected bool _OpacityDirty = false;


        /// <summary>
        /// The container which this element is a child of. Can be null if top level UI object
        /// </summary>
        protected UIContainer _Parent;
        
        /// <summary>
        /// Matrix object for LocalToGlobal calculations. Converts a relative coordinate
        /// into a global screen coordinate. Essentially, its Parent.Matrix + Matrix.Offset(_X, _Y)
        /// </summary>
        protected float[] _Mtx = Matrix2D.IDENTITY;

        protected Vector2 _Scale = Vector2.One;
        protected Vector2 _ScaleParent = Vector2.One;

        /// <summary>
        /// Indicates if something has changed to make the Matrix invalid, e.g. X,Y has changed or
        /// the component is now inside a different parent object
        /// </summary>
        protected bool _MtxDirty;

        /// <summary>
        /// Indicates if the component is visible or not. If false the UIElement
        /// should not draw
        /// </summary>
        public bool Visible = true;



        public UIElement()
        {
        }

        public string ID
        {
            get { return m_StringID; }
            set { m_StringID = value; }
        }


        /// <summary>
        /// X coordinate of this component relative to its parent
        /// </summary>
        public float X
        {
            get
            {
                return _X;
            }
            set
            {
                _X = value;
                _MtxDirty = true;
            }
        }


        /// <summary>
        /// Y coordinate of this component relative to its parent
        /// </summary>
        public float Y
        {
            get
            {
                return _Y;
            }
            set
            {
                _Y = value;
                _MtxDirty = true;
            }
        }


        public float ScaleX
        {
            get { return _ScaleX; }
            set { _ScaleX = value; _MtxDirty = true; }
        }

        public float ScaleY
        {
            get { return _ScaleY; }
            set { _ScaleY = value; _MtxDirty = true; }
        }

        public float Opacity
        {
            get {
                return _Opacity;
            }
            set
            {
                _Opacity = value;
                _OpacityDirty = true;
            }
        }

        public Color OpacityColor
        {
            get
            {
                if (_OpacityDirty)
                {
                    CalculateOpacity();
                }
                return _OpacityColor;
            }
        }

        /// <summary>
        /// Returns the size of the component.
        /// This is used for utilities such as mouse hit testing etc
        /// </summary>
        public virtual Vector2 Size
        {
            get
            {
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// The container which this element is a child of. Can be null if top level UI object
        /// </summary>
        public UIContainer Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Parent = value;
                _MtxDirty = true;
            }
        }
        
        /// <summary>
        /// Matrix object for LocalToGlobal calculations. Converts a relative coordinate
        /// into a global screen coordinate. Essentially, its Parent.Matrix + Matrix.Offset(_X, _Y)
        /// </summary>
        public float[] Matrix
        {
            get
            {
                return _Mtx;
            }
        }

        public Vector2 Scale
        {
            get { return _Scale; }
        }


        protected virtual void CalculateOpacity()
        {
            if (_Parent != null)
            {
                _OpacityColor = _Parent.OpacityColor;
            }
            else
            {
                _OpacityColor = Color.White;
            }

            _OpacityColor.A = (byte)((((float)_OpacityColor.A / 255.0f) * _Opacity) * 255);
            _OpacityDirty = false;
        }



        /// <summary>
        /// Calculate a matrix which represents this objects position in space
        /// </summary>
        protected virtual void CalculateMatrix()
        {
            if (_Parent != null)
            {
                _Mtx = _Parent.Matrix.CloneMatrix();
                _ScaleParent = _Parent.Scale;
            }
            else
            {
                _ScaleParent = Vector2.One;
                _Mtx = Matrix2D.IDENTITY;
            }

            _Mtx.Translate(_X, _Y);
            _Mtx.Scale(_ScaleX, _ScaleY);


            //if (_ScaleX != 1 || _ScaleY != 1)
            //{
                //var scale = Matrix.CreateScale(_ScaleX, _ScaleY, 1.0f);
                //_Mtx *= scale;
            //}
            //_Mtx *= Matrix.CreateTranslation(_X, _Y, 0);
            _Scale = _Mtx.ExtractScaleVector();
            
            /*new Vector2(
                (float)Math.Sqrt((_Mtx.M11 * _Mtx.M11) + (_Mtx.M12 * _Mtx.M12) + (_Mtx.M13 * _Mtx.M13)),
                (float)Math.Sqrt((_Mtx.M21 * _Mtx.M21) + (_Mtx.M22 * _Mtx.M22) + (_Mtx.M23 * _Mtx.M23))
            );*/

            _InvertedMtx = null;
            _MtxDirty = false;
            _HitTestCache.Clear();
        }


        public void InvalidateMatrix()
        {
            _MtxDirty = true;
        }

        public void InvalidateOpacity()
        {
            _OpacityDirty = true;
        }

        public int Depth { get; set; }

        /// <summary>
        /// Standard UIElement update method. All sub-classes should call
        /// this super method
        /// </summary>
        /// <param name="statex"></param>
        public virtual void Update(UpdateState state)
        {
            this.Depth = state.Depth++;

            if (_MtxDirty)
            {
                CalculateMatrix();
            }
            if (_OpacityDirty)
            {
                CalculateOpacity();
            }

            if (m_MouseRefs != null)
            {
                foreach (var mouseRegion in m_MouseRefs)
                {
                    if (HitTestArea(state, mouseRegion.Region, true))
                    {
                        state.MouseEvents.Add(mouseRegion);
                    }
                }
            }
        }

        public abstract void Draw(SpriteBatch batch);


        /// <summary>
        /// Converts a local rectangle to a screen global rectangle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public Rectangle LocalRect(float x, float y, float w, float h)
        {
            return LocalRect(x, y, w, h, _Mtx);
        }

        /// <summary>
        /// Converts a local point to a screen global point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 LocalPoint(float x, float y)
        {
            return LocalPoint(new Vector2(x, y));
        }

        public Vector2 LocalPoint(Vector2 point)
        {
            //var zero = Vector2.Transform(Vector2.Zero, _Mtx);
            //zero.X *= _ScaleParent.X;
            //zero.Y *= _ScaleParent.Y;
            //var v1 = Vector2.Transform(zero, Matrix.CreateTranslation(point.X, point.Y, 0));

            //Vector2 v1 = new Vector2(point.X, point.Y);
            //v1 = Vector2.Transform(v1, _Mtx);
            //v1.X *= _ScaleParent.X;
            //v1.Y *= _ScaleParent.Y;
            //Vector2.Transform(to, this.Matrix)

            return _Mtx.TransformPoint(point);
        }

        public Vector2 GlobalPoint(Vector2 globalPoint)
        {
            if (_InvertedMtx == null)
            {
                _InvertedMtx = _Mtx.Invert();
            }
            return _InvertedMtx.TransformPoint(globalPoint);
        }

        public Rectangle LocalRect(float x, float y, float w, float h, float[] mtx)
        {
            mtx.TransformPoint(ref x, ref y);
            w *= _Scale.X;
            h *= _Scale.Y;

            /*
            Vector2 v1 = new Vector2(x, y);
            v1 = Vector2.Transform(v1, mtx);

            v1.X *= _ScaleParent.X;
            v1.Y *= _ScaleParent.Y;
            w *= _Scale.X;
            h *= _Scale.Y;
            */
            return new Rectangle((int)x, (int)y, (int)w, (int)h);
        }

        /// <summary>
        /// Draw a string onto the UI
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="style"></param>
        public void DrawLocalString(SpriteBatch batch, string text, Vector2 to, TextStyle style)
        {
            var scale = _Scale;
            if (style.Scale != 1.0f)
            {
                scale = new Vector2(scale.X * style.Scale, scale.Y * style.Scale);
            }
            batch.DrawString(style.SpriteFont, text, LocalPoint(to), style.Color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Draw a string with alignment
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="style"></param>
        /// <param name="bounds"></param>
        /// <param name="align"></param>
        public void DrawLocalString(SpriteBatch batch, string text, Vector2 to, TextStyle style, Rectangle bounds, TextAlignment align)
        {
            DrawLocalString(batch, text, to, style, bounds, align, Rectangle.Empty);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="style"></param>
        /// <param name="bounds"></param>
        /// <param name="align"></param>
        /// <param name="margin"></param>
        public void DrawLocalString(SpriteBatch batch, string text, Vector2 to, TextStyle style, Rectangle bounds, TextAlignment align, Rectangle margin)
        {
            //TODO: We should find some way to cache this data

            var scale = _Scale;
            if (style.Scale != 1.0f)
            {
                scale = new Vector2(scale.X * style.Scale, scale.Y * style.Scale);
            }

            Vector2 size = style.SpriteFont.MeasureString(text) * style.Scale;

            if (margin != Rectangle.Empty)
            {
                bounds.X += margin.X;
                bounds.Y += margin.Y;
                bounds.Width -= margin.Right;
                bounds.Height -= margin.Bottom;
            }

            var pos = to;
            pos.X += bounds.X;
            pos.Y += bounds.Y;

            if ((align & TextAlignment.Right) == TextAlignment.Right)
            {
                pos.X += (bounds.Width - size.X);
            }
            else if ((align & TextAlignment.Center) == TextAlignment.Center)
            {
                pos.X += (bounds.Width - size.X) / 2;
            }

            if ((align & TextAlignment.Middle) == TextAlignment.Middle)
            {
                pos.Y += (bounds.Height - size.Y) / 2;
            }
            else if ((align & TextAlignment.Bottom) == TextAlignment.Bottom)
            {
                pos.Y += (bounds.Height - size.Y);
            }

            pos = LocalPoint(pos);
            batch.DrawString(style.SpriteFont, text, pos, style.Color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }


        /// <summary>
        /// Draws a texture to the UIElement. This method will deal with
        /// the matrix calculations
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="texture"></param>
        /// <param name="to"></param>
        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Vector2 to)
        {
            /**
             * v1.X *= _ScaleParent.X;
             * v1.Y *= _ScaleParent.Y;
             */
            batch.Draw(texture, LocalPoint(to), null, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale, SpriteEffects.None, 0.0f);
        }

        /// <summary>
        /// Draws a texture to the UIElement. This method will deal with
        /// the matrix calculations
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="texture"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Rectangle from, Vector2 to)
        {
            batch.Draw(texture, LocalPoint(to), from, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale, SpriteEffects.None, 0.0f);
        }

        /// <summary>
        /// Draws a texture to the UIElement. This method will deal with
        /// the matrix calculations
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="texture"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="scale"></param>
        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Nullable<Rectangle> from, Vector2 to, Vector2 scale)
        {
            batch.Draw(texture, LocalPoint(to), from, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale * scale, SpriteEffects.None, 0.0f);
        }


        private Dictionary<Rectangle, Vector4> _HitTestCache = new Dictionary<Rectangle, Vector4>();

        /// <summary>
        /// Returns true if the mouse is over the given area
        /// </summary>
        /// <param name="state"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool HitTestArea(UpdateState state, Rectangle area, bool cache)
        {
            if (!Visible) { return false; }


            var globalLeft = 0.0f;
            var globalTop = 0.0f;
            var globalRight = 0.0f;
            var globalBottom = 0.0f;

            if (_HitTestCache.ContainsKey(area))
            {
                var positions = _HitTestCache[area];
                globalLeft = positions.X;
                globalTop = positions.Y;
                globalRight = positions.Z;
                globalBottom = positions.W;
            }
            else
            {
                var globalPosition = LocalRect(area.X, area.Y, area.Width, area.Height);

                /*var globalPosition = _Mtx.TransformPoint(area.X, area.Y);//Vector2.Transform(new Vector2(area.X, area.Y), this.Matrix);
                globalLeft = globalPosition.X * _ScaleParent.X;
                globalTop = globalPosition.Y * _ScaleParent.Y;
                globalRight = globalLeft + (area.Width * _Scale.X);
                globalBottom = globalTop + (area.Height * _Scale.Y);*/

                if (cache)
                {
                    _HitTestCache.Add(area, new Vector4(globalPosition.X, globalPosition.Y, globalPosition.Right, globalPosition.Bottom));
                }
            }

            var mx = state.MouseState.X;
            var my = state.MouseState.Y;

            if (mx >= globalLeft && mx <= globalRight &&
                my >= globalTop && my <= globalBottom)
            {
                return true;
            }

            return false;
        }





        private List<UIMouseEventRef> m_MouseRefs;

        /**
         * Mouse utilities
         */
        public UIMouseEventRef ListenForMouse(Rectangle region, UIMouseEvent callback)
        {
            var newRegion = new UIMouseEventRef()
            {
                Callback = callback,
                Region = region,
                Element = this
            };
            if (m_MouseRefs == null)
            {
                m_MouseRefs = new List<UIMouseEventRef>();
            }
            m_MouseRefs.Add(newRegion);

            return newRegion;
        }

        private float[] _InvertedMtx;

        /// <summary>
        /// Gets the local mouse coordinates for the given mouse state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Vector2 GetMousePosition(MouseState mouse)
        {
            if (_InvertedMtx == null)
            {
                _InvertedMtx = _Mtx.Invert();
            }

            return _InvertedMtx.TransformPoint(mouse.X, mouse.Y);
        }


        public Texture2D GetTexture(uint id_0, uint id_1)
        {
            ulong ID = (ulong)(((ulong)id_0)<<32 | ((ulong)(id_1 >> 32)));
            return GetTexture(ID);
        }

        public static uint[] MASK_COLORS = new uint[]{
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };


        public static Texture2D StoreTexture(ulong id, ContentResource assetData)
        {
            return StoreTexture(id, assetData, true, false);
        }


        public static Texture2D StoreTexture(ulong id, ContentResource assetData, bool mask, bool cacheOnDisk)
        {
            /**
             * This may not be the right way to get the texture to load as ARGB but it works :S
             */
            Texture2D texture = null;
            using (var stream = new MemoryStream(assetData.Data, false))
            {
                var isCached = assetData.FromCache;

                if (mask && !isCached)
                {
                    var textureParams = Texture2D.GetCreationParameters(GameFacade.GraphicsDevice, stream);
                    textureParams.Format = SurfaceFormat.Color;

                    stream.Seek(0, SeekOrigin.Begin);
                    texture = Texture2D.FromFile(GameFacade.GraphicsDevice, stream, textureParams);

                    TextureUtils.ManualTextureMaskSingleThreaded(ref texture, MASK_COLORS);
                    
                }
                else
                {
                    texture = Texture2D.FromFile(GameFacade.GraphicsDevice, stream);
                }
                UI_TEXTURE_CACHE.Add(id, texture);

                if (cacheOnDisk && !isCached)
                {
                    /** Cache the texture to the file system **/
                    var filePath = GameFacade.CacheDirectory + "/" + id + ".dds";
                    texture.Save(filePath, ImageFileFormat.Dds);
                    GameFacade.Cache.AddFile(id, File.ReadAllBytes(filePath));
                }

                return texture;
            }
        }


        private static Dictionary<ulong, Texture2D> UI_TEXTURE_CACHE = new Dictionary<ulong, Texture2D>();
        public static Texture2D GetTexture(ulong id)
        {
            try
            {
                if (UI_TEXTURE_CACHE.ContainsKey(id))
                {
                    return UI_TEXTURE_CACHE[id];
                }

                var assetData = ContentManager.GetResourceInfo(id);
                //var textureParams = new TextureCreationParameters();
                //textureParams.Format = SurfaceFormat.Rgb32;

                
                return StoreTexture(id, assetData);
            }
            catch (Exception ex)
            {
            }
            return null;
        }



        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        protected void ManualTextureMask(ref Texture2D Texture, Color ColorFrom)
        {
            Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == ColorFrom)
                    data[i] = ColorTo;
            }

            Texture.SetData(data);
        }

        protected void ManualTextureMask(ref Texture2D Texture, Color[] ColorsFrom)
        {
                        Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                foreach (Color Clr in ColorsFrom)
                {
                    if (data[i] == Clr)
                        data[i] = ColorTo;
                }
            }

            Texture.SetData(data);
        }



        public override string ToString()
        {
            var clazzName = this.GetType().Name;

            if (m_StringID == null)
            {
                return clazzName;
            }
            return clazzName + "(" + m_StringID + ")";
        }


        /// <summary>
        /// Gets the bounding box for the component
        /// </summary>
        /// <returns></returns>
        public virtual Rectangle GetBounds()
        {
            return Rectangle.Empty;
        }







        /**
         * UIScript setters
         */
        [UIAttribute("position")]
        public Vector2 Position
        {
            set
            {
                _X = value.X;
                _Y = value.Y;
                InvalidateMatrix();
            }
            get
            {
                return new Vector2(_X, _Y);
            }
        }





        /// <summary>
        /// Little utility to make it easier to do work outside of the UI thread
        /// </summary>
        public void Async(AsyncHandler handler)
        {
            var t = new Thread(new ThreadStart(handler));
            t.Start();
        }

        public delegate void AsyncHandler();

    }


    public enum UIMouseEventType
    {
        MouseOver,
        MouseOut,
        MouseDown,
        MouseUp
    }

    public delegate void UIMouseEvent(UIMouseEventType type, UpdateState state);


    public class UIMouseEventRef
    {
        public UIMouseEvent Callback;
        public Rectangle Region;
        public UIElement Element;
        public UIMouseEventType LastState;
    }
}
