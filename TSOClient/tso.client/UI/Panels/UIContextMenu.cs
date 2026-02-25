using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.UI.Panels
{
    public class UIContextMenu : UIContainer, IFocusableUI
    {
        public bool IsFocused { get; set; }
        public int TabIndex { get; set; } = -1;
        public UIElement Watching;
        public string LastSearch;
        private int Height;
        private int Width = 200;

        public UIContextMenu(UIElement anchor, IEnumerable<UIContextMenuItem> items, UIContainer parent = null)
        {
            Watching = anchor;

            int length = items.Count();
            Width = length == 0 ? 200 : items.Max(item => item.PreferredWidth);

            int i = 0;

            foreach (var item in items)
            {
                item.Width = Width;
                item.Y = (i++) * 22;

                if (i == length)
                {
                    item.Last = true;
                }

                Add(item);
            }

            Height = length * 22;

            if (parent is UICachedContainer cached)
            {
                cached.DynamicOverlay.Add(this);
            }
            else
            {
                (parent ?? anchor.Parent).Add(this);
            }

            GameFacade.Screens.inputManager.SetFocus(this);
        }

        private ButtonState _lastPressed;

        public override void Update(UpdateState state)
        {
            int xPos = Parent.LocalPoint(Watching.Position).X + Width > UIScreen.Current.ScreenWidth ?
                ((int)Watching.Size.X - Width) :
                0;

            Position = Watching.Position + new Vector2(xPos, Watching.Size.Y);
            base.Update(state);

            // if the mouse was pressed outside the context menu, instantly close it.

            ButtonState pressed = state.MouseState.LeftButton;

            if (pressed == ButtonState.Pressed && _lastPressed == ButtonState.Released)
            {
                var point = GlobalPoint(state.MouseState.Position.ToVector2());

                if (point.X < 0 || point.Y < 0 || point.X > Width || point.Y > Height)
                {
                    Close();
                    return;
                }
            }

            _lastPressed = pressed;

            if (Visible)
            {
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Down))
                    MoveSelection(1);
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Up))
                    MoveSelection(-1);
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Enter))
                    Select();
                if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Escape))
                    Close();
            }
        }

        public bool Select()
        {
            var bestOption = (UIContextMenuItem)Children.FirstOrDefault(x => ((UIContextMenuItem)x).Selected);
            if (bestOption != null)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
                bestOption.OnSelect?.Invoke();
                Close();
                return true;
            }

            return false;
        }

        public void MoveSelection(int off)
        {
            var i = Children.FindIndex(x => ((UIContextMenuItem)x).Selected);
            var ni = i + off;
            if (ni >= Children.Count || ni < 0) return;
            if (i != -1)
            {
                ((UIContextMenuItem)Children[i]).Selected = false;
            }
            ((UIContextMenuItem)Children[ni]).Selected = true;
        }

        public void ClearSelection()
        {
            foreach (UIContextMenuItem child in Children)
                child.Selected = false;
        }

        public void Close()
        {
            if (Parent != null)
            {
                if (Parent is UICachedContainer cached)
                {
                    cached.DynamicOverlay.Remove(this);
                }
                else
                {
                    Parent.Remove(this);
                }

                Parent = null;
            }
        }

        public void OnFocusChanged(FocusEvent newFocus)
        {
            // If we lose focus, close the context menu.

            if (newFocus == FocusEvent.FocusOut && Parent != null)
            {
                Close();
            }
        }
    }

    public class UIContextMenuItem : UIElement
    {
        public Texture2D PxWhite;
        public bool Last;
        public bool Selected;
        public string Caption;
        public TextStyle Style;
        public UIMouseEventRef ClickHandler;
        public int PreferredWidth;

        public int Width;
        public Action OnSelect;

        public UIContextMenuItem(string caption, Action onSelect)
        {
            PxWhite = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            Style = TextStyle.DefaultLabel.Clone();
            Style.Size = 8;
            Caption = caption;
            OnSelect = onSelect;

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 200, 22), new UIMouseEvent(MouseEvent));

            PreferredWidth = (int)Style.MeasureString(caption).X + 26;
            Width = PreferredWidth;
        }

        public void MouseEvent(UIMouseEventType type, UpdateState state)
        {
            var owner = Parent as UIContextMenu;

            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    owner.ClearSelection();
                    Selected = true;
                    break;
                case UIMouseEventType.MouseDown:
                    HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
                    OnSelect?.Invoke();
                    owner.Close();
                    break;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, PxWhite, null, Vector2.Zero, new Vector2(Width, 22), new Color(57, 85, 117));
            if (Selected) DrawLocalTexture(batch, PxWhite, null, Vector2.Zero, new Vector2(Width, 22), Color.White * 0.25f);
            DrawLocalString(batch, Caption, new Vector2(3, 3), Style);
            if (!Last) DrawLocalTexture(batch, PxWhite, null, new Vector2(0, 21), new Vector2(Width, 1), Color.White * 0.5f);
        }
    }
}
