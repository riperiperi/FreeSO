using FSO.Client;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.UI
{
    public class CommentContainer : UIContainer
    {
        public UIImage Background;
        public UILabel ClickLabel;
        public UITextEdit TextEdit;
        public event Action<string> OnCommentChanged;

        public UITweenInstance BgTween;
        public UITweenInstance HideTween;
        public UIMouseEventRef AddCommentListener;

        public string Comment;
        public bool CommentEmpty => Comment == null || Comment.Length == 0;
        public bool Collapsed;
        public bool Hidden;
        public float _InvalidationDummy;
        public float InvalidationDummy {
            get => _InvalidationDummy;
            set
            {
                Invalidate();
                _InvalidationDummy = value;
            }
        }

        public bool MouseOver;
        public int MouseOverTimer;
        private bool CommentChanged;

        public CommentContainer(string comment)
        {
            Background = new UIImage(EditorResource.Get().CommentBubble).With9Slice(15, 15, 15, 15);
            Add(Background);

            ClickLabel = new UILabel();
            ClickLabel.CaptionStyle = EditorResource.Get().CommentStyle;
            ClickLabel.Alignment = TextAlignment.Center;
            ClickLabel.Size = Vector2.One;
            ClickLabel.Position = new Vector2(15, -22);
            Add(ClickLabel);

            TextEdit = new UITextEdit();
            TextEdit.TextStyle = ClickLabel.CaptionStyle;
            TextEdit.Alignment = TextAlignment.Left;
            TextEdit.SetSize(200, 15);
            TextEdit.Position = new Vector2(0, -1000);
            TextEdit.OnChange += TextEdit_OnChange;
            TextEdit.OnFocusOut += TextEdit_OnFocusOut;
            
            Add(TextEdit);
            
            SetComment(comment);
            if (Collapsed)
            {
                Hidden = true;
                Visible = false;
            }
        }

        private void TextEdit_OnFocusOut(UIElement element)
        {
            if (CommentChanged)
            {
                OnCommentChanged?.Invoke(Comment);
                CommentChanged = false;
            }
        }

        public void ToggleHidden(bool hidden)
        {
            if (Hidden == hidden) return;
            Visible = true;
            Hidden = hidden;
            if (HideTween != null) HideTween.Complete();
            if (hidden)
            {
                if (CommentEmpty) ToggleCollapsed(true);
                //remove mouse handler
                //tween scale and opacity to 0
                GameFacade.Screens.Tween.To(this, 0.2f, new Dictionary<string, float>()
                {
                    { "ScaleX", 0 },
                    { "ScaleY", 0 },
                    { "Opacity", 0 },
                    { "InvalidationDummy", 0 },
                }, TweenQuad.EaseIn);
                if (AddCommentListener != null) RemoveMouseListener(AddCommentListener);
            }
            else
            {
                //add mouse handler
                //tween scale and opacity to 1
                GameFacade.Screens.Tween.To(this, 0.2f, new Dictionary<string, float>()
                {
                    { "ScaleX", 1 },
                    { "ScaleY", 1 },
                    { "Opacity", 1 },
                    { "InvalidationDummy", 1 },
                }, TweenQuad.EaseOut);
                if (Collapsed) {
                    AddCommentListener = ListenForMouse(new Rectangle(-5, -30, 35, 35), MouseEvent);
                }
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (!Hidden && CommentEmpty && state.InputManager.GetFocus() != TextEdit)
            {
                //hide this commment if the mouse timer expires (kept alive by mousing over this or the parent prim box)

                if (MouseOver) MouseOverTimer = 3;
                else if (MouseOverTimer-- <= 0)
                {
                    ToggleHidden(true);
                }
            }
        }

        public void MouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseDown:
                    //expand
                    state.InputManager.SetFocus(TextEdit);
                    ToggleCollapsed(false);
                    break;
                case UIMouseEventType.MouseOver:
                    ToggleHidden(false);
                    MouseOver = true;
                    MouseOverTimer = 3;
                    break;
                case UIMouseEventType.MouseOut:
                    MouseOver = false;
                    break;
            }
        }

        private void TextEdit_OnChange(UIElement element)
        {
            Comment = TextEdit.CurrentText;
            CommentChanged = true;
            ResizeBasedOnTextEdit();
        }

        public void SetComment(string comment)
        {
            Comment = comment;
            TextEdit.CurrentText = comment;
            ToggleCollapsed(CommentEmpty);
            ResizeBasedOnTextEdit();
        }

        public void ToggleCollapsed(bool collapsed)
        {
            Collapsed = collapsed;
            ClickLabel.Caption = (CommentEmpty) ? "+" : "..";
            ClickLabel.Visible = Collapsed;
            TextEdit.Visible = !Collapsed;
            if (Collapsed) SetSize(new Rectangle());
            else ResizeBasedOnTextEdit();
        }

        public int LinesToHeight(int lines)
        {
            return (int)Math.Ceiling(TextEdit.TextStyle.MeasureString("W").Y * lines);
        }

        public void ResizeBasedOnTextEdit()
        {
            //first change the width to match the number of characters input
            //18 ws to 200px width. start at 30 and scale up

            var targWidth = Math.Max(4, Math.Min(TextEdit.TextStyle.MeasureString(TextEdit.CurrentText).X + 1, 200));
            if (TextEdit.Width != targWidth)
            {
                TextEdit.SetSize(targWidth, TextEdit.Height);
            }

            TextEdit.VerticalScrollPosition = 0;
            TextEdit.ComputeDrawingCommands();
            var height = LinesToHeight(TextEdit.TotalLines);
            if (TextEdit.Height != height)
            {
                TextEdit.SetSize(TextEdit.Width, height);
                TextEdit.ComputeDrawingCommands();
            } 
            var bounds = TextEdit.GetBounds();
            TextEdit.Position = new Vector2(13, -(6 + bounds.Height));
            SetSize(bounds);
        }

        public void SetSize(Rectangle rect)
        {
            if (BgTween != null) BgTween.Complete();
            var margin = 7;
            var minWidth = Collapsed ? 30 : 50;
            var width = Math.Max(minWidth, rect.Width + margin * 2 + 12);
            var height = Math.Max(30, rect.Height + margin * 2);
            BgTween = GameFacade.Screens.Tween.To(Background, 0.25f,
                new Dictionary<string, float>() {
                    { "Width", width },
                    { "Height", height },
                    { "Y", -height }
                }, TweenQuad.EaseOut);
        }

        public void ShadDraw(UISpriteBatch batch)
        {
            if (!Visible) return;
            var blend = Background.BlendColor;
            Background.BlendColor = new Color((byte)0, (byte)0, (byte)0, (byte)(blend.A * 0.2f));
            Background.Position += new Vector2(5, 5);
            Background.CalculateMatrix();
            Background.Draw(batch);
            Background.Position -= new Vector2(5, 5);
            Background.CalculateMatrix();
            Background.BlendColor = blend;
        }
    }
}
