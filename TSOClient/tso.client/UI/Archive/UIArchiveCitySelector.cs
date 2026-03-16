using FSO.Client.GameContent;
using FSO.Client.Model.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    public class UIArchiveCitySelector : UIDialog
    {
        private const int StatusOnlineWidth = 153;
        private const int TruncateCityNameWidth = 43;
        private const int ListboxResizeX = StatusOnlineWidth + TruncateCityNameWidth;

        private readonly string[] BuiltinCityNames = [
            "Blazing Falls",
            "Alphaville",
            "Test Center",
            "Interhogan",
            "Ocean's Edge",
            "East Jerome",
            "Fancey Fields",
            "Betaville",
            "Charvatia",
            "Dragon's Cove",
            "Rancho Rizzo",
            "Zavadaville",
            "Queen Margaret’s",
            "Shannopolis",
            "Grantley Grove",
            "Calvin’s Creek",
            "Billabong",
            "Mount Fuji",
            "Dan’s Grove",
            "Jolly Pines",
            "Yatesport",
            "Landry Lakes",
            "Nichol's Notch",
            "King Canyons",
            "Virginia Islands",
            "Pixie Point",
            "West Darrington",
            "Upper Shankelston",
            "Albertstown",
            "Terra Tablante",
            ];

        //Positioned & sized by UIScript
        public UIImage CityListBoxBackground { get; set; }
        public UIImage CityDescriptionBackground { get; set; }

        //Set by UIScript
        public Texture2D CityIconImage { get; set; }
        public Texture2D thumbnailBackgroundImage { get; set; }
        public Texture2D thumbnailAlphaImage { get; set; }
        public UIListBox CityListBox { get; set; }
        public UISlider CityListSlider { get; set; }
        public UIButton CityListScrollUpButton { get; set; }
        public UIButton CityScrollDownButton { get; set; }

        public UITextEdit DescriptionText { get; set; }
        public UISlider CityDescriptionSlider { get; set; }
        public UIButton CityDescriptionScrollUpButton { get; set; }
        public UIButton CityDescriptionDownButton { get; set; }

        public UIButton OkButton { get; set; }
        public UIButton CancelButton { get; set; }

        // Sort buttons
        public UIButton NameSortButton { get; set; }
        public UIButton OnlineSortButton { get; set; }
        public UIButton StatusSortButton { get; set; }


        /** Strings **/
        public string OnlineStatusUp { get; set; }
        public string OnlineStatusDown { get; set; }
        public string StatusBusy { get; set; }
        public string StatusFull { get; set; }
        public string StatusBusyFull { get; set; }
        public string StatusOk { get; set; }

        public string CityReservedDialogTitle { get; set; }
        public string CityReservedDialogMessage { get; set; }
        public string CityFullDialogTitle { get; set; }
        public string CityFullDialogMessage { get; set; }
        public string CityBusyDialogTitle { get; set; }
        public string CityBusyDialogMessage { get; set; }

        //Internal
        private UIImage CityThumb { get; set; }

        private UIListBoxTextStyle ListStyleNormal;
        private Texture2D SimIconShared;

        private UITextBox NameInput;
        private UITextEdit DescriptionInput;
        private bool AutoName = true;

        private readonly ArchiveManifest Template;
        private readonly Dictionary<int, Dictionary<string, string>> CityCST = [];

        public event Action<ArchiveManifest> OnResult;

        public UIArchiveCitySelector(ArchiveManifest template)
            : base(UIDialogStyle.Standard, true)
        {
            Template = template;
            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;

            SimIconShared = custom.Get("archive_simshared.png").Get(gd);
            CityListBoxBackground = new UIImage(UITextBox.StandardBackground);
            this.Add(CityListBoxBackground);
            CityDescriptionBackground = new UIImage(UITextBox.StandardBackground);
            this.Add(CityDescriptionBackground);

            var script = this.RenderScript("cityselector.uis");
            this.DialogSize = (Point)script.GetControlProperty("DialogSize");

            var cityThumbBG = new UIImage(thumbnailBackgroundImage);
            cityThumbBG.Position = (Vector2)script.GetControlProperty("CityThumbnailBackgroundPosition");
            this.Add(cityThumbBG);
            CityThumb = new UIImage();
            CityThumb.Position = (Vector2)script.GetControlProperty("CityThumbnailPosition");
            this.Add(CityThumb);

            CityDescriptionSlider.AttachButtons(CityDescriptionScrollUpButton, CityDescriptionDownButton, 1);
            DescriptionText.AttachSlider(CityDescriptionSlider);

            OkButton.Disabled = true;
            OkButton.OnButtonClick += new ButtonClickDelegate(OkButton_OnButtonClick);
            CancelButton.OnButtonClick += new ButtonClickDelegate(CancelButton_OnButtonClick);

            this.Caption = (string)script["TitleString"];

            // Reposition everything to fit the city configuration
            CityListBox.SetSize(CityListBox.Width - ListboxResizeX, CityListBox.Height);
            /*
            CityListSlider.Position -= new Vector2(ListboxResizeX, 0);
            CityListScrollUpButton.Position -= new Vector2(ListboxResizeX, 0);
            CityScrollDownButton.Position -= new Vector2(ListboxResizeX, 0);
            */
            CityListBox.Position += new Vector2(ListboxResizeX, 0);
            CityListBoxBackground.Position += new Vector2(ListboxResizeX, 0);
            NameSortButton.Position += new Vector2(ListboxResizeX, 0);
            CityListBoxBackground.Size -= new Vector2(ListboxResizeX, 0);
            NameSortButton.Size -= new Vector2(TruncateCityNameWidth, 0);
            CityListBox.Columns.RemoveRange(CityListBox.Columns.Count - 2, 2);

            Remove(OnlineSortButton);
            Remove(StatusSortButton);
            Remove(CityListBox);
            DynamicOverlay.Add(CityListBox);

            // Archive city configuration
            var saveVbox = new UIVBoxContainer();
            saveVbox.Position = new Vector2(25, 39);

            saveVbox.Add(new UILabel()
            {
                Caption = "Save name:"
            });

            saveVbox.Add(NameInput = new UITextBox()
            {
                Size = new Microsoft.Xna.Framework.Vector2(166, 25),
                CurrentText = "New Save",
            });

            saveVbox.Add(new UILabel()
            {
                Caption = "Description:"
            });

            saveVbox.Add(DescriptionInput = new UITextEdit()
            {
                Size = new Microsoft.Xna.Framework.Vector2(166, 158),
                CurrentText = "",
                BackgroundTextureReference = UITextBox.StandardBackground,
                ScrollbarImage = GetTexture(0x4AB00000001),
                ScrollbarGutter = 4,
                TextMargin = new Rectangle(8, 2, 8, 3),
                MaxChars = 4096,
            });

            saveVbox.AutoSize();

            Add(saveVbox);

            DescriptionInput.InitDefaultSlider();

            NameInput.OnChange += NameChange;

            /** Parse the list styles **/
            ListStyleNormal = script.Create<UIListBoxTextStyle>("CityListBoxColors", CityListBox.FontStyle);


            CityListSlider.AttachButtons(CityListScrollUpButton, CityScrollDownButton, 1);

            CityListBox.TextStyle = ListStyleNormal;
            CityListBox.AttachSlider(CityListSlider);
            CityListBox.OnChange += new ChangeDelegate(CityListBox_OnChange);

            CityListBox.Items = BuildShards();

            if (CityListBox.Items.Count > 0) {
                CityListBox.SelectedIndex = 0;
            }
        }

        private void NameChange(UIElement element)
        {
            AutoName = false;

            var name = NameInput.CurrentText;

            OkButton.Disabled = name.Length == 0 || NameTaken(name);
        }

        private string GetPath(string name)
        {
            return Path.Combine("Content/ArchiveCities", string.Join('_', name.Split(Path.GetInvalidFileNameChars())));
        }

        private bool NameTaken(string name)
        {
            var path = GetPath(name);

            return Path.Exists(path);
        }

        private string GenerateAutoName()
        {
            var map = SelectedMap;

            if (map == null || !int.TryParse(map, out int id))
            {
                return null;
            }

            var basename = GetCityName(id);
            var name = basename;

            int copyNumber = 2;
            while (NameTaken(name))
            {
                name = $"{basename} ({copyNumber++})";
            }

            return name;
        }

        private string GetCityText(int id, string key)
        {
            if (!CityCST.TryGetValue(id, out var cst))
            {
                var dir = Content.Content.Get().CityMaps.GetDir(id);

                string path = dir == null ? null : Path.Combine(dir, "info.cst");

                if (path == null || !File.Exists(path))
                {
                    cst = new()
                    {
                        { "1", "Unknown City" },
                        { "2", "This city data is missing info.cst, so it doesn't have a name or description." },
                    };
                }
                else
                {
                    cst = ContentStrings.ReadTable(path);
                }

                CityCST[id] = cst;
            }

            cst.TryGetValue(key, out var value);

            return value ?? "???";
        }

        private string GetCityName(int id)
        {
            var fsoMap = id >= 100;

            return fsoMap ? GetCityText(id, "1") : BuiltinCityNames[id - 1];
        }

        private List<UIListBoxItem> BuildShards()
        {
            var ids = Content.Content.Get().CityMaps.ListIDs();
            var result = new List<UIListBoxItem>();

            foreach (var id in ids)
            {
                var fsoMap = id >= 100;

                result.Add(new UIListBoxItem(id.ToString().PadLeft(4, '0'), fsoMap ? SimIconShared : CityIconImage, GetCityName(id))
                {
                    CustomStyle = ListStyleNormal
                });
            }

            return result;
        }


        void CancelButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
            OnResult?.Invoke(null);
        }

        void OkButton_OnButtonClick(UIElement button)
        {
            // Copy the template into the target folder, and initialize the city.

            var srcFolder = Path.GetDirectoryName(Template.ActivePath);

            string name = NameInput.CurrentText;
            string description = DescriptionInput.CurrentText;

            var dstFolder = GetPath(name);

            CopyDirectory(srcFolder, dstFolder);

            var newTemplate = new ArchiveManifest(Path.Combine(dstFolder, "archive.ini"))
            {
                Name = name,
                Description = description,
                Map = SelectedMap,
                Template = false
            };
            newTemplate.LocalDir = "data/";

            newTemplate.Save();

            // Update the shard

            Visible = false;

            var factory = new ArchiveServerFactory(ArchiveServerFactory.GetQuickStartConfig(), null);
            factory.Prepare(newTemplate, (success) =>
            {
                if (success)
                {
                    new ArchiveManagement(factory.GetConfig()).SetInfo(newTemplate.Name, newTemplate.Map);

                    OnResult(newTemplate);
                    UIScreen.RemoveDialog(this);
                }
                else
                {
                    Visible = true;
                    Directory.Delete(dstFolder, true);
                    UIAlert.Alert("Unknown error", "Failed to create the save file from template.", true);
                }
            });
        }

        private static void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);

            foreach (var file in Directory.GetFiles(src))
            {
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)));
            }

            foreach (var dir in Directory.GetDirectories(src))
            {
                CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
            }
        }

        public string SelectedMap
        {
            get
            {
                if (CityListBox.SelectedItem != null)
                {
                    return (string)CityListBox.SelectedItem.Data;
                }

                return null;
            }
        }

        /// <summary>
        /// Handle when a user selects a city
        /// </summary>
        /// <param name="element"></param>
        void CityListBox_OnChange(UIElement element)
        {
            var selectedItem = CityListBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            if (AutoName)
            {
                var auto = GenerateAutoName();
                if (auto != null)
                {
                    NameInput.CurrentText = auto;
                    OkButton.Disabled = false;
                }
            }

            var map = (string)selectedItem.Data;

            String gamepath = GameFacade.GameFilePath("");


            var fsoMap = int.Parse(map) >= 100;

            var cityThumb = (fsoMap) ?
            Path.Combine(FSOEnvironment.ContentDir, "Cities/city_" + map + "/thumbnail.png")
            : GameFacade.GameFilePath("cities/city_" + map + "/thumbnail.bmp");

            //Take a copy so we dont change the original when we alpha mask it
            Texture2D cityThumbTex = TextureUtils.Copy(GameFacade.GraphicsDevice, TextureUtils.TextureFromFile(
               GameFacade.GraphicsDevice, cityThumb));
            TextureUtils.CopyAlpha(ref cityThumbTex, thumbnailAlphaImage);

            CityThumb.Texture = cityThumbTex;
            DescriptionText.CurrentText = fsoMap ? GetCityText(int.Parse(map), "2") : GameFacade.Strings.GetString("238", int.Parse(map).ToString());
            DescriptionText.VerticalScrollPosition = 0;
        }
    }
}
