using FSO.Client.Controllers;
using FSO.Client.Rendering.City;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Graphics;
using Ninject;
using System;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UILotThumbButtonAuto : UILotThumbButton
    {
        public Binding<Lot> Property { get; internal set; }
        //Mixing concerns here but binding avatar id is much nicer than lots of plumbing each time
        private IClientDataService DataService;
        private LotThumbEntry ThumbLock;
        private Texture2D LastThumb;
        public event Action<uint, string> OnNameChange;

        private uint _LotId = uint.MaxValue;
        public uint LotId
        {
            get { return _LotId; }
            set
            {
                _LotId = value;

                if (ThumbLock != null) ThumbLock.Held--;
                ThumbLock = FindController<CoreGameScreenController>()?.Terrain?.LockLotThumb(value);

                if (value == uint.MaxValue || value == 0)
                {
                    GameThread.NextUpdate((x) =>
                    {
                        Property.Value = null;
                    });
                }
                else
                {
                    DataService.Get<Lot>(_LotId).ContinueWith(x =>
                    {
                        if (x.Result == null) { return; }
                        Property.Value = x.Result;
                    });
                    DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, _LotId);
                }
            }
        }

        public string LotTooltip
        {
            set
            {
                VisitorButton.Tooltip = value;
                RoommateButton.Tooltip = value;
            }
        }

        private string _LotName = "";
        public string LotName
        {
            get { return _LotName; }
            set
            {
                LotTooltip = value;
                OnNameChange?.Invoke(LotId, value);
                _LotName = value;
            }
        }

        public UILotThumbButtonAuto()
        {
            DataService = FSOFacade.Kernel.Get<IClientDataService>();

            Property = new Binding<Lot>()
                .WithBinding(this, "LotName", "Lot_Name");
        }

        public UILotThumbButtonAuto WithDefaultClick()
        {
            OnLotClick += (btn) => { FindController<CoreGameScreenController>()?.ShowLotPage(LotId); };
            return this;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (ThumbLock != null && LastThumb != ThumbLock.LotTexture)
            {
                SetThumbnail(ThumbLock.LotTexture, LotId);
                LastThumb = ThumbLock.LotTexture;
            }
        }

        public override void Removed()
        {
            Property.Dispose();
            if (ThumbLock != null) ThumbLock.Held--;
        }
    }
}
