using FSO.Client.Model.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Rendering.Framework.IO;
using FSO.Server.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveConfigExportDialog : UIDialog
    {
        public UITextBox PathInput;
        public UIButton ExportButton;
        private bool ArchiveAbsolute;
        private bool TSOAbsolute = true;

        private ArchiveConfiguration Config;
        private ArchiveManifest Manifest;

        public UIArchiveConfigExportDialog(ArchiveConfiguration config, ArchiveManifest manifest) : base(UIDialogStyle.Close, true)
        {
            Config = config;
            Manifest = manifest;

            Caption = GameFacade.Strings.GetString("f128", "36");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "37"),
                Wrapped = true
            });

            desc.Size = new Vector2(350, 140);

            var pathBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            pathBox.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "44")
            });

            pathBox.Add(PathInput = new UITextBox() { });

            vbox.Add(pathBox);

            vbox.Add(new UISpacer(1, 8));

            var flagsVbox = new UIVBoxContainer();

            CreateCheck(flagsVbox, GameFacade.Strings.GetString("f128", "38"), ArchiveAbsolute, (bool value) => { ArchiveAbsolute = value; });
            CreateCheck(flagsVbox, GameFacade.Strings.GetString("f128", "39"), TSOAbsolute, (bool value) => { TSOAbsolute = value; });

            flagsVbox.AutoSize();

            vbox.Add(flagsVbox);

            var vbox2 = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };

            vbox.Add(new UISpacer(1, 8));

            vbox2.Add(ExportButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "40")
            });

            vbox2.AutoSize(); //TODO: somehow force horiz size from parent?

            vbox.Add(vbox2);

            Add(vbox);

            PathInput.SetSize(350, 25);
            PathInput.CurrentText = Path.GetFullPath("config.json");

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += (elem) =>
            {
                UIScreen.RemoveDialog(this);
            };

            ExportButton.OnButtonClick += Export;
        }

        private void Export(UIElement button)
        {
            var factory = new ArchiveServerFactory(Config, null);
            factory.Prepare(Manifest, (success) =>
            {
                if (success)
                {
                    var json = ArchiveConfigExporter.BuildAndExport(Config, ArchiveAbsolute, TSOAbsolute);

                    var path = PathInput.CurrentText;

                    try
                    {
                        var ext = Path.GetExtension(path);

                        if (ext == null)
                        {
                            // Assume the user gave a directory
                            path = Path.Combine(path, "config.json");
                        }

                        // Ensure the directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        {
                            using var file = File.Open(path, FileMode.Create);
                            using var writer = new StreamWriter(file);

                            writer.Write(json);
                        }

                        bool clipboardSuccess = true;
                        try
                        {
                            ClipboardHandler.Default.Set(path);
                        }
                        catch (Exception)
                        {
                            clipboardSuccess = false;
                        }

                        UIAlert.Alert(
                            GameFacade.Strings.GetString("f128", "47"),
                            GameFacade.Strings.GetString("f128", clipboardSuccess ? "45" : "43", [path]),
                            true);
                    }
                    catch (Exception)
                    {
                        UIAlert.Alert(GameFacade.Strings.GetString("f128", "41"), GameFacade.Strings.GetString("f128", "42", [path]), true);
                    }
                }
            });
        }

        private void CreateCheck(UIContainer target, string label, bool defaultValue, Action<bool> onChanged)
        {
            var flagHbox = new UIHBoxContainer();

            var check = new UIButton(GetTexture(0x0000083600000001));
            check.Selected = defaultValue;

            flagHbox.Add(check);

            check.OnButtonClick += (elem) =>
            {
                check.Selected = !check.Selected;
                onChanged(check.Selected);
            };

            flagHbox.Add(new UILabel()
            {
                Caption = label,
            });

            /*
            if (flag.HelpAction != null)
            {
                UIButton helpBtn = new UIButton(HelpButtonTexture);
                var helpAction = flag.HelpAction;
                helpBtn.OnButtonClick += (elem) => helpAction();
                flagHbox.Add(helpBtn);
            }
            */

            flagHbox.AutoSize();

            target.Add(flagHbox);
        }
    }
}
