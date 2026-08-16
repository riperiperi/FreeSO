using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveServerPorts : UIArchiveDialog
    {
        public UITextBox LotInput;
        public UITextBox CityInput;

        public UIArchiveServerPorts(ArchiveConfiguration config, Action onClose) : base(UIDialogStyle.OK, true)
        {
            Caption = GetString("250");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = GetString("251"),
                Wrapped = true
            });

            desc.Size = new Vector2(300, 70);

            var cityPortBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            cityPortBox.Add(new UILabel()
            {
                Caption = GetString("252")
            });

            cityPortBox.Add(CityInput = new UITextBox() { });

            vbox.Add(cityPortBox);

            var lotPortBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            lotPortBox.Add(new UILabel()
            {
                Caption = GetString("253")
            });

            lotPortBox.Add(LotInput = new UITextBox() { });

            vbox.Add(lotPortBox);

            Add(vbox);

            LotInput.SetSize(100, 25);
            CityInput.SetSize(100, 25);

            LotInput.CurrentText = config.LotPort.ToString();
            CityInput.CurrentText = config.CityPort.ToString();

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);

            OKButton.OnButtonClick += (elem) =>
            {
                onClose();
                UIScreen.RemoveDialog(this);
            };
        }

        public bool GetCityPort(out ushort port)
        {
            return ushort.TryParse(CityInput.CurrentText, out port);
        }

        public bool GetLotPort(out ushort port)
        {
            return ushort.TryParse(LotInput.CurrentText, out port);
        }
    }
}
