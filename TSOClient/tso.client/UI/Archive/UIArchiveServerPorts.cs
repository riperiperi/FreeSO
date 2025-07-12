using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using Microsoft.Xna.Framework;
using System;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveServerPorts : UIDialog
    {
        public UITextBox LotInput;
        public UITextBox CityInput;

        public UIArchiveServerPorts(ArchiveConfiguration config, Action onClose) : base(UIDialogStyle.OK, true)
        {
            Caption = "Custom Ports";
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = "Choose TCP ports for the server. For public access, these ports should be forwarded in your router settings.",
                Wrapped = true
            });

            desc.Size = new Vector2(300, 70);

            var cityPortBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            cityPortBox.Add(new UILabel()
            {
                Caption = "City: "
            });

            cityPortBox.Add(CityInput = new UITextBox() { });

            vbox.Add(cityPortBox);

            var lotPortBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            lotPortBox.Add(new UILabel()
            {
                Caption = "Lot: "
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
