using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Series of containers that auto size, saves a bunch of work whenever we need to make
    /// debug / custom UI
    /// </summary>
    
    public class UIHBoxContainer : UIAbstractDynamicContainer
    {
        public float Spacing { get; set; } = 5;
        public UIContainerVerticalAlignment VerticalAlignment { get; set; } = UIContainerVerticalAlignment.Top;

        public override void AutoSize()
        {
            base.AutoSize();

            lock (Children)
            {
                var height = (float)0;

                foreach (var child in Children)
                {
                    height = Math.Max(child.Size.Y, height);
                }

                var x = (float)0;

                foreach (var child in Children)
                {
                    var y = (float)0;
                    switch (VerticalAlignment)
                    {
                        case UIContainerVerticalAlignment.Middle:
                            y = (height - child.Size.Y) / 2;
                            break;
                        case UIContainerVerticalAlignment.Bottom:
                            y = height - child.Size.Y;
                            break;
                    }

                    child.Position = new Microsoft.Xna.Framework.Vector2(x, y);

                    x += child.Size.X;
                    x += Spacing;
                }

                _Size = new Microsoft.Xna.Framework.Vector2(x - Spacing, height);
            }
        }
    }

    public class UIVBoxContainer : UIAbstractDynamicContainer
    {
        public float Spacing { get; set; } = 5;
        public UIContainerHorizontalAlignment HorizontalAlignment { get; set; } = UIContainerHorizontalAlignment.Left;

        public override void AutoSize()
        {
            base.AutoSize();

            lock (Children)
            {
                var width = (float)0;

                foreach (var child in Children)
                {
                    width = Math.Max(child.Size.X, width);
                }

                var y = (float)0;

                foreach (var child in Children)
                {
                    var x = (float)0;
                    switch (HorizontalAlignment)
                    {
                        case UIContainerHorizontalAlignment.Center:
                            x = (width - child.Size.X) / 2;
                            break;
                        case UIContainerHorizontalAlignment.Right:
                            x = width - child.Size.X;
                            break;
                    }

                    child.Position = new Microsoft.Xna.Framework.Vector2(x, y);

                    y += child.Size.Y;
                    y += Spacing;
                }

                _Size = new Microsoft.Xna.Framework.Vector2(width, y - Spacing);
            }
        }
    }

    public abstract class UIAbstractDynamicContainer : UIContainer, IUIAutoSize
    {
        protected Vector2 _Size = Vector2.Zero;

        public override Vector2 Size
        {
            get
            {
                return _Size;
            }
            set
            {
            }
        }

        public virtual void AutoSize()
        {
            lock (Children)
            {
                foreach (var child in Children)
                {
                    if(child is IUIAutoSize){
                        ((IUIAutoSize)child).AutoSize();
                    }
                }
            }
        }
    }

    public enum UIContainerVerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    public enum UIContainerHorizontalAlignment
    {
        Left,
        Center,
        Right
    }
}
