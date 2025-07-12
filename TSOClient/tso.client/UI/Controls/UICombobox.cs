using FSO.Client.Controllers;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.UI.Controls
{
    public struct UIComboboxItem
    {
        public string Name;
        public object Value;
    }

    /// <summary>
    /// Kind of hacks the message inbox dropdown into a combobox.
    /// Can be resized horizontally.
    /// </summary>
    public class UICombobox : UIContainer
    {
        public object SelectedItem
        {
            get
            {
                return MenuListBox.SelectedItem?.Data;
            }
            set
            {
                Select(value);
            }
        }

        public int SelectedIndex
        {
            get
            {
                return MenuListBox.SelectedIndex;
            }
            set
            {
                MenuListBox.SelectedIndex = value;
            }
        }

        private int? _width;
        public int? Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
                UpdateSize();
            }
        }

        public override Vector2 Size
        {
            get => new Vector2(Width ?? 340, 24);
            set => Width = (int)value.X;
        }

        public Texture2D backgroundCollapsedImage { get; set; }
        public Texture2D backgroundExpandedImage { get; set; }

        public UIButton DropDownButton { get; set; }

        public UIButton MenuScrollUpButton { get; set; }
        public UIButton MenuScrollDownButton { get; set; }
        public UISlider MenuSlider { get; set; }

        public UIListBox MenuListBox { get; set; }
        public UITextEdit MenuTextEdit { get; set; }

        public UIImage Background;
        public bool open;

        private List<UIComboboxItem> _items = new List<UIComboboxItem>();
        public List<UIComboboxItem> Items
        {
            get { return _items; }
            set
            {
                _items = value;

                MenuListBox.Items.Clear();
                if (value != null)
                {
                    MenuListBox.Items.AddRange(value.Select(x =>
                    {
                        return new UIListBoxItem(x.Value, new object[] { x.Name });
                    }));
                }

                MenuListBox.Items = MenuListBox.Items;
            }
        }

        UIScript Script;

        public event Action<object> OnSelect;

        public UICombobox()
        {
            Script = this.RenderScript("messageinboxmenu.uis");
            Background = new UIImage(backgroundCollapsedImage).With9Slice(40, 40, 6, 6);
            this.AddAt(0, Background);

            open = true;
            ToggleOpen();

            DropDownButton.OnButtonClick += new ButtonClickDelegate(DropDownButton_OnButtonClick);
            DropDownButton.Tooltip = null;
            MenuTextEdit.Mode = UITextEditMode.ReadOnly;

            MenuListBox.AttachSlider(MenuSlider);
            MenuSlider.AttachButtons(MenuScrollUpButton, MenuScrollDownButton, 1f);

            MenuListBox.OnChange += SelectComboboxElement;

            UpdateSize();
        }

        private void UpdateSize()
        {
            int width = Width ?? 340;

            if (open)
            {
                Background.SetSize(width, backgroundExpandedImage.Height);
            }
            else
            {
                Background.SetSize(width, backgroundCollapsedImage.Height);
            }

            MenuTextEdit.SetSize(width - 45, MenuTextEdit.Height);
            MenuListBox.SetSize(width - 49, MenuListBox.Height);
            DropDownButton.X = width - 21;
            MenuSlider.X = width - 19;
            MenuScrollUpButton.X = width - 23;
            MenuScrollDownButton.X = width - 23;
        }

        private void SelectComboboxElement(UIElement button)
        {
            UpdateComboText();

            var selected = MenuListBox.SelectedItem;
            if (selected == null) return;

            OnSelect?.Invoke(selected.Data);

            if (open)
            {
                ToggleOpen();
            }
        }


        void DropDownButton_OnButtonClick(UIElement button)
        {
            ToggleOpen();
        }

        public void ToggleOpen()
        {
            Invalidate();
            int width = Width ?? 340;

            if (open)
            {
                Background.Texture = backgroundCollapsedImage;
                Background.With9Slice(40, 40, 6, 6);
                Background.SetSize(width, backgroundCollapsedImage.Height);
            }
            else
            {
                Background.Texture = backgroundExpandedImage;
                Background.With9Slice(40, 40, 50, 25);
                Background.SetSize(width, backgroundExpandedImage.Height);
            }

            open = !open;
            MenuSlider.Visible = open;
            MenuScrollUpButton.Visible = open;
            MenuScrollDownButton.Visible = open;
            MenuListBox.Visible = open;
        }

        public void Select(object value)
        {
            // Try find the value in the current items
            if (value == null)
            {
                MenuListBox.SelectedItem = null;
            }
            else
            {
                MenuListBox.SelectedIndex = _items.FindIndex(x => x.Value == value);
            }

            UpdateComboText();
        }

        private void UpdateComboText()
        {
            var selected = MenuListBox.SelectedIndex;

            if (selected == -1)
            {
                MenuTextEdit.CurrentText = "";
            }
            else
            {
                MenuTextEdit.CurrentText = _items[selected].Name;
            }
        }
    }
}
