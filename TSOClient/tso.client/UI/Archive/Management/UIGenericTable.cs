using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive.Management
{
    internal struct UITableColumn
    {
        public string Label;
        public int Width;
        public TextAlignment Aligngment;

        public UITableColumn(string label, int width, TextAlignment aligngment = TextAlignment.Left | TextAlignment.Middle)
        {
            Label = label;
            Width = width;
            Aligngment = aligngment;
        }
    }

    internal class UIGenericTable : UIContainer
    {
        private const int ColumnLegendHeight = 20;
        private const int SliderWidth = 20;

        public List<UIListBoxItem> Items
        {
            get
            {
                return _listBox.Items;
            }
            set
            {
                _listBox.Items = value; 
            }
        }

        public UIListBoxItem SelectedItem
        {
            get
            {
                return _listBox.SelectedItem;
            }
            set
            {
                _listBox.SelectedItem = value;
            }
        }

        public int SelectedIndex
        {
            get
            {
                return _listBox.SelectedIndex;
            }
            set
            {
                _listBox.SelectedIndex = value;
            }
        }

        public bool Loading
        {
            get
            {
                return _statusLabel.Visible;
            }
            set
            {
                _statusLabel.Visible = value; 
            }
        }

        public override Vector2 Size { get; set; }
        public event ChangeDelegate OnChange;

        private readonly UIImage _background;
        private readonly UIListBox _listBox;
        private readonly UILabel _statusLabel;
        private readonly List<UITableColumn> _columns;
        private List<UILabel> _columnLabels;

        public UIGenericTable(List<UITableColumn> columns, int height = 300)
        {
            _columns = columns;

            var gd = GameFacade.GraphicsDevice;
            var ui = Content.Content.Get().CustomUI;

            var searchFont = TextStyle.DefaultLabel.Clone();
            searchFont.Size = 8;

            _background = new UIImage(ui.Get("archive_translist.png").Get(gd)).With9Slice(13, 13, 13, 13);
            _background.Position = new Vector2(0, ColumnLegendHeight);
            _background.SetSize(180, 300);
            Add(_background);

            var textStyle = new UIListBoxTextStyle(searchFont)
            {
                SelectedColor = Color.Black,
                HighlightedColor = new Color(255, 255, 255),
                DisabledColor = new Color(150, 150, 150)
            };

            Add(_listBox = new UIListBox()
            {
                Position = _background.Position + new Vector2(10, 10),
                Mask = true,
                Columns = GenerateColumns(),
                RowHeight = 20,
                TextStyle = textStyle,
                SelectionFillColor = new Color(250, 200, 140),
                ScrollbarImage = GetTexture(0x31000000001),
                ScrollbarGutter = 12,
                UseChildElements = true,
            });

            var statusStyle = TextStyle.DefaultLabel.Clone();
            statusStyle.Shadow = true;

            Add(_statusLabel = new UILabel()
            {
                Caption = "Loading...",
                Position = _listBox.Position,
                Size = _listBox.Size,
                Wrapped = true,
                Alignment = TextAlignment.Center | TextAlignment.Middle,
                CaptionStyle = statusStyle,
            });

            _listBox.InitDefaultSlider();
            SetSize(_columns.Sum((col) => col.Width) + SliderWidth + 20, height);
            PopulateColumnLabels();

            _listBox.OnChange += _listBox_OnChange;
        }

        private void _listBox_OnChange(UIElement element)
        {
            OnChange?.Invoke(this);
        }

        public void SetSize(int width, int height)
        {
            _background.SetSize(width - SliderWidth, height - ColumnLegendHeight);
            _listBox.Size = _background.Size - new Vector2(20, 20);
            _statusLabel.Size = _statusLabel.Size;
            _listBox.VisibleRows = (int)Math.Ceiling(_listBox.Height / _listBox.RowHeight);
            _listBox.PositionChildSlider();

            Size = new Vector2(width, height);
        }

        private UIListBoxColumnCollection GenerateColumns()
        {
            var result = new UIListBoxColumnCollection();

            foreach (var column in _columns)
            {
                result.Add(new UIListBoxColumn() { Width = column.Width, Alignment = column.Aligngment });
            }

            return result;
        }

        private void PopulateColumnLabels()
        {
            if (_columnLabels != null)
            {
                foreach (var label in _columnLabels)
                {
                    Remove(label);
                    _columnLabels.Remove(label);
                }

                _columnLabels.Clear();
            }
            else
            {
                _columnLabels = new List<UILabel>();
            }

            int totalWidth = 0;
            foreach (var col in _columns)
            {
                var label = new UILabel()
                {
                    Caption = col.Label,
                    Position = new Vector2(_listBox.X + totalWidth, 0),
                };

                Add(label);
                _columnLabels.Add(label);

                totalWidth += col.Width;
            }
        }
    }
}
