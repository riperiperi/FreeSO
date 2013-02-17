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
        protected Matrix _Mtx = Matrix.Identity;

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

        public UIElement(string StrID)
        {
            m_StringID = StrID;
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
        public Matrix Matrix
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
                _Mtx = _Parent.Matrix;
                _ScaleParent = _Parent.Scale;
            }
            else
            {
                _ScaleParent = Vector2.One;
                _Mtx = Matrix.Identity;
            }

            if (_ScaleX != 1 || _ScaleY != 1)
            {
                var scale = Matrix.CreateScale(_ScaleX, _ScaleY, 1.0f);
                _Mtx *= scale;
            }
            _Mtx *= Matrix.CreateTranslation(_X, _Y, 0);
            
            _Scale = new Vector2(
                (float)Math.Sqrt((_Mtx.M11 * _Mtx.M11) + (_Mtx.M12 * _Mtx.M12) + (_Mtx.M13 * _Mtx.M13)),
                (float)Math.Sqrt((_Mtx.M21 * _Mtx.M21) + (_Mtx.M22 * _Mtx.M22) + (_Mtx.M23 * _Mtx.M23))
            );
            
            _MtxDirty = false;
        }


        public void InvalidateMatrix()
        {
            _MtxDirty = true;
        }

        public void InvalidateOpacity()
        {
            _OpacityDirty = true;
        }

        /// <summary>
        /// Standard UIElement update method. All sub-classes should call
        /// this super method
        /// </summary>
        /// <param name="statex"></param>
        public virtual void Update(UpdateState statex)
        {
            if (_MtxDirty)
            {
                CalculateMatrix();
            }
            if (_OpacityDirty)
            {
                CalculateOpacity();
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
            return LocalRect(x, y, w, h, this.Matrix);
        }

        /// <summary>
        /// Converts a local point to a screen global point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 LocalPoint(float x, float y)
        {
            Vector2 v1 = new Vector2(x, y);
            v1 = Vector2.Transform(v1, _Mtx);
            v1.X *= _ScaleParent.X;
            v1.Y *= _ScaleParent.Y;
            return v1;
        }

        public Vector2 LocalPoint(Vector2 point)
        {
            Vector2 v1 = new Vector2(point.X, point.Y);
            v1 = Vector2.Transform(v1, _Mtx);
            v1.X *= _ScaleParent.X;
            v1.Y *= _ScaleParent.Y;
            return v1;
        }




        public Rectangle LocalRect(float x, float y, float w, float h, Matrix mtx)
        {
            Vector2 v1 = new Vector2(x, y);
            v1 = Vector2.Transform(v1, mtx);

            v1.X *= _ScaleParent.X;
            v1.Y *= _ScaleParent.Y;
            w *= _Scale.X;
            h *= _Scale.Y;

            return new Rectangle((int)v1.X, (int)v1.Y, (int)w, (int)h);
        }


        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Vector2 to)
        {
            batch.Draw(texture, Vector2.Transform(to, this.Matrix), null, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale, SpriteEffects.None, 0.0f);
        }

        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Rectangle from, Vector2 to)
        {
            batch.Draw(texture, Vector2.Transform(to, this.Matrix), from, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale, SpriteEffects.None, 0.0f);
        }

        public void DrawLocalTexture(SpriteBatch batch, Texture2D texture, Rectangle from, Vector2 to, Vector2 scale)
        {
            batch.Draw(texture, Vector2.Transform(to, this.Matrix), from, _OpacityColor, 0.0f,
                    new Vector2(0.0f, 0.0f), _Scale * scale, SpriteEffects.None, 0.0f);
        }


        public Texture2D GetTexture(uint id_0, uint id_1)
        {
            ulong ID = (ulong)(((ulong)id_0)<<32 | ((ulong)(id_1 >> 32)));
            return GetTexture(ID);
        }

        public Texture2D GetTexture(ulong id)
        {
            var assetData = ContentManager.GetResourceFromLongID(id);
            Texture2D texture = Texture2D.FromFile(GameFacade.GraphicsDevice, new MemoryStream(assetData));
            //TextureUtils.ManualTextureMask(ref texture, MASK_COLOR);

            return texture;
        }




        public string StrID
        {
            get { return m_StringID; }
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
    }
}
