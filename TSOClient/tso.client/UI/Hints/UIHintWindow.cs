using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Client.UI.Hints
{
    public class UIHintWindow : UIDialog
    {
        public UIImage InnerBackground;
        public List<UIHint> AllHints;

        public List<UIHintListItem> Categories = new List<UIHintListItem>();
        public UIListBox ListBox;
        public UISlider ResultsSlider;
        public UIButton SliderUpButton;
        public UIButton SliderDownButton;

        public UIHintListItem LastSelected;
        public UILabel Title;

        public UIHintWindow() : base(UIDialogStyle.Close, true)
        {
            Caption = "Hints";
            SetSize(800, 600);

            CloseButton.OnButtonClick += (btn) => { UIScreen.RemoveDialog(this); };
            Opacity = 1f;

            InnerBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            InnerBackground.Position = new Vector2(15, 45);
            InnerBackground.SetSize(240, 530);
            AddAt(3, InnerBackground);

            AllHints = FSOFacade.Hints.LoadAllHints();
            var cats = AllHints.GroupBy(x => x.Category).OrderBy(x => x.FirstOrDefault(y => y.CatOrder != null)?.CatOrder ?? 0);

            int index = 0;
            foreach (var cat in cats)
            {
                var item = new UIHintListItem(true, cat.FirstOrDefault().Category, 0);
                item.ChildItems = new List<UIHintListItem>();
                foreach (var hint in cat)
                {
                    var subitem = new UIHintListItem(false, hint.Title, index++);
                    subitem.Hint = hint;
                    item.ChildItems.Add(subitem);
                }
                Categories.Add(item);
            }

            ListBox = new UIListBox() { Position = new Vector2(15 + 7, 45 + 6) };
            ListBox.SetSize(227, 530 - 12);
            ListBox.RowHeight = 24;
            ListBox.Columns = new UIListBoxColumnCollection();
            ListBox.Columns.Add(new UIListBoxColumn() { Width = 227 });
            ListBox.SelectionFillColor = new Color(250, 200, 140);
            ListBox.OnChange += ListBox_OnChange;
            Add(ListBox);
            
            ResultsSlider = new UISlider();
            ResultsSlider.Orientation = 1;
            ResultsSlider.Texture = GetTexture(0x31000000001);
            ResultsSlider.MinValue = 0;
            ResultsSlider.MaxValue = 2;

            ResultsSlider.X = 260;
            ResultsSlider.Y = 51;
            ResultsSlider.SetSize(0, 514f);
            Add(ResultsSlider);

            SliderUpButton = new UIButton(GetTexture(0x31200000001));
            SliderUpButton.Position = new Vector2(257, 44);
            Add(SliderUpButton);
            SliderDownButton = new UIButton(GetTexture(0x31100000001));
            SliderDownButton.Position = new Vector2(257, 566);
            Add(SliderDownButton);

            ResultsSlider.AttachButtons(SliderUpButton, SliderDownButton, 1f);
            ListBox.AttachSlider(ResultsSlider);

            Icon = new UIImage();
            Icon.Position = new Vector2(290, 65);
            Icon.SetSize(0, 0);
            Add(Icon);

            Title = new UILabel();
            Title.Position = Icon.Position = new Vector2(290, 40);
            Title.Size = new Vector2(492, 15);
            Title.CaptionStyle = TextStyle.DefaultTitle;
            Title.CaptionStyle = Title.CaptionStyle.Clone();
            Title.CaptionStyle.Color = Color.White;
            Title.CaptionStyle.Size = 14;
            Title.CaptionStyle.Shadow = true;
            Title.Alignment = TextAlignment.Center | TextAlignment.Middle;
            Add(Title);

            RenderCategories();
        }

        private bool m_TextDirty = false;
        public override void CalculateMatrix()
        {
            base.CalculateMatrix();
            foreach (var cat in Categories)
            {
                cat.InvalidateMatrix();
                foreach (var item in cat.ChildItems)
                {
                    item.InvalidateMatrix();
                }
            }
            m_TextDirty = true;
        }

        private void ListBox_OnChange(UIElement element)
        {
            if (ListBox.SelectedIndex == -1) return;
            if (LastSelected != null)
            {
                LastSelected.Deselect();
                LastSelected = null;
            }
            HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Click);
            var item = ListBox.Items[ListBox.SelectedIndex];
            var label = (UIHintListItem)item.Columns[0];
            if (label.Category)
            {
                label.Expanded = !label.Expanded;
                RenderCategories();
            }
            else
            {
                ShowHint(label.Hint);
                LastSelected = label;
                label.Select();
            }
        }

        public void RenderCategories()
        {
            var items = new List<UIListBoxItem>();
            foreach (var cat in Categories)
            {
                items.Add(new UIListBoxItem(cat.Category, cat));
                if (cat.Expanded && cat.ChildItems != null)
                {
                    foreach (var sub in cat.ChildItems)
                    {
                        items.Add(new UIListBoxItem(AllHints[sub.Index], sub));
                    }
                }
            }
            ListBox.Items = items;
        }

        public void ShowHint(UIHint hint)
        {

            Icon.Texture?.Dispose();

            Icon.Texture = null;
            if (hint.Image != null && hint.Image != "")
            {
                //try load the image for this hint
                SetIcon(null, 0, 0);
                try
                {
                    if (hint.Image.Length > 0 && hint.Image[0] == '@')
                    {
                        
                        using (var strm = File.Open(Content.Content.Get().GetPath("uigraphics/hints/" + hint.Image.Substring(1)), FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var tex = ImageLoader.FromStream(GameFacade.GraphicsDevice, strm);
                            SetIcon(tex, tex.Width, tex.Height);
                        }
                    }
                    else
                    {
                        using (var strm = File.Open("Content/UI/hints/images/" + hint.Image, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var tex = ImageLoader.FromStream(GameFacade.GraphicsDevice, strm);
                            SetIcon(tex, tex.Width, tex.Height);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            Title.Caption = hint.Title;
            ComputeText(hint);
        }

        public void SetIcon(Texture2D img, int width, int height)
        {
            Icon.Texture = img;
            IconSpace = new Vector2(width + 15, height);

            if (img == null)
            {
                IconSpace = new Vector2();
                Icon.SetSize(0, 0);
            }
            else
            {
                float scale = Math.Min(1, Math.Min((float)height / (float)img.Height, (float)width / (float)img.Width));
                Icon.SetSize(img.Width * scale, img.Height * scale);
                Icon.Position = new Vector2(290, 65) + new Vector2(width / 2 - (img.Width * scale / 2), height / 2 - (img.Height * scale / 2));
            }
        }

        private Vector2 IconSpace;
        private TextRendererResult m_MessageText;
        private UIImage Icon;
        private UIHint ActiveHint;

        private void ComputeText(UIHint hint)
        {
            var msg = hint.Body;
            msg = GameFacade.Emojis.EmojiToBB(msg);
            m_MessageText = TextRenderer.ComputeText(msg, new TextRendererOptions
            {
                Alignment = TextAlignment.Left | TextAlignment.Top,
                MaxWidth = 492,
                Position = new Vector2(290, 65),
                Scale = _Scale,
                TextStyle = TextStyle.DefaultLabel,
                WordWrap = true,
                TopLeftIconSpace = IconSpace,
                BBCode = true
            }, this);
            ActiveHint = hint;
            m_TextDirty = false;
        }


        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if (m_MessageText != null)
            {
                if (m_TextDirty && ActiveHint != null) ComputeText(ActiveHint);
                TextRenderer.DrawText(m_MessageText.DrawingCommands, this, batch);
            }
        }
    }
}
