using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Hints
{
    public class UIHintListItem : UIContainer
    {
        public int Indent = 16;
        public UILabel Label;
        public Texture2D WhitePx;
        public bool Category;
        public string Name;

        public Color Col = Color.TransparentBlack;
        public int Index;

        public List<UIHintListItem> ChildItems;
        public bool Expanded = true;
        public bool Selected;
        public UIHint Hint;

        public Texture2D ContractedIcon;
        public Texture2D ExpandedIcon;
        public bool Colored = false;

        public UIHintListItem(bool cat, string caption, int index)
        {
            Category = cat;
            Index = index;
            WhitePx = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            Label = new UILabel() { Caption = caption };
            Label.CaptionStyle = Label.CaptionStyle.Clone();

            if (Category)
            {
                Label.CaptionStyle.Color = Color.White;
                Label.CaptionStyle.Size = 11;
                Col = new Color(31, 46, 63);
            } else
            {
                if (Index % 2 == 1) Col = new Color(31, 46, 63) * 0.25f;
                Label.CaptionStyle.Size = 9;
            }
            Label.Size = new Vector2(227-Indent, 24);
            Label.X += 7+Indent;
            Label.Alignment = TextAlignment.Middle | TextAlignment.Left;
            Add(Label);

            var ui = Content.Content.Get().CustomUI;
            ContractedIcon = ui.Get("hcat_hidden.png").Get(GameFacade.GraphicsDevice);
            ExpandedIcon = ui.Get("hcat_expand.png").Get(GameFacade.GraphicsDevice);
        }

        private UpdateState LastState;
        public override void Update(UpdateState state)
        {
            base.Update(state);
            LastState = state;
        }

        public override void Draw(UISpriteBatch batch)
        {
            Visible = true; //hack to get things to draw correctly in the listbox
            if (!Colored && Hint != null)
            {
                Label.CaptionStyle.Color = (FSOFacade.Hints.ShownGUIDs.Contains(Hint.GUID)) ? Color.LightBlue : TextStyle.DefaultLabel.Color;
                Colored = true;
            }
            //InvalidateMatrix();
            PreDraw(batch);
            //if (LastState != null) Update(LastState);
            if (!Selected) DrawLocalTexture(batch, WhitePx, null, new Vector2(), new Vector2(227, 24), Col);
            if (Category) DrawLocalTexture(batch, Expanded ? ExpandedIcon : ContractedIcon, new Vector2(7, 7));
            base.Draw(batch);
        }

        public void Select()
        {
            Label.CaptionStyle.Color = Color.Black;
            Selected = true;
        }

        public void Deselect()
        {
            Label.CaptionStyle.Color = (FSOFacade.Hints.ShownGUIDs.Contains(Hint.GUID))?Color.LightBlue:TextStyle.DefaultLabel.Color;
            Selected = false;
        }
    }
}
