using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIPropertySelectContainer : UIContainer
    {
        private UILabel SelectedLotName;
        private UIInboxDropdown Dropdown;
        public uint SelectedLot;

        public UIPropertySelectContainer()
        {
            Add(SelectedLotName = new UILabel()
            {
                Size = new Vector2(341, 25),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                Caption = "<no property selected>"
            });

            Add(Dropdown = new UIInboxDropdown() { Position = new Vector2(0, 25) });
            Dropdown.OnSearch += (query) =>
            {
                FindController<GenericSearchController>()?.SearchLots(query, false, (results) =>
                {
                    Dropdown.SetResults(results);
                });
            };
            Dropdown.OnSelect += SelectLot; ;
            Add(Dropdown);

            var ctr = ControllerUtils.BindController<GenericSearchController>(this);
        }

        private void SelectLot(uint id, string name)
        {
            SelectedLot = id;
            SelectedLotName.Caption = name;
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, 341, 80);
        }
    }
}
