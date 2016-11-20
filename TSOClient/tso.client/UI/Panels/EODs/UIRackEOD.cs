using FSO.Client.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIRackEOD : UIAbstractRackEOD
    {
        public UIButton btnTryOn { get; set; }
        public UIButton btnPurchase { get; set; }

        public UIRackEOD(UIEODController controller) : base(controller, "rackeod.uis")
        {
        }

        protected override void InitUI()
        {
            base.InitUI();

            btnTryOn.OnButtonClick += BtnTryOn_OnButtonClick;
        }

        private void BtnTryOn_OnButtonClick(Framework.UIElement button)
        {
            var selectedOutfit = GetSelectedOutfit();
            if (selectedOutfit == null) { return; }

            Send("rack_try_outfit_on", selectedOutfit.outfit_id.ToString());
        }

        protected override EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short
            };
        }
    }
}
