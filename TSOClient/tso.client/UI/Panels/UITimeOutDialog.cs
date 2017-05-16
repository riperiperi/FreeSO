using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Client.UI.Panels
{
    class UITimeOutDialog : UIDialog
    {
        /// <summary>
        /// Exit buttons
        /// </summary>
        public UIButton CloseButton { get; set; }
        public UILabel CounterText { get; set; }
        public VM CallingVM;
        public int Timer;
        public int SubTimer;

        public UITimeOutDialog(VM callingVM, int timer)
            : base(UIDialogStyle.Standard, true)
        {
            this.RenderScript("timeoutdialog.uis");
            this.SetSize(380, 180);

            CloseButton.OnButtonClick += CloseButton_OnButtonClick;
            CallingVM = callingVM;
            Timer = timer;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            SubTimer++;
            if (SubTimer >= FSOEnvironment.RefreshRate)
            {
                Timer--;
                if (Timer <= 0) ForceDC();
                UpdateTimer();
                SubTimer = 0;
            }
        }

        private void UpdateTimer()
        {
            CounterText.Caption = (Timer/(60*60)).ToString().PadLeft(2, '0') + ":"+((Timer/60)%60).ToString().PadLeft(2, '0')+":"+(Timer%60).ToString().PadLeft(2, '0');
        }

        private void CloseButton_OnButtonClick(Framework.UIElement button)
        {
            CallingVM.SendCommand(new VMNetTimeoutNotifyCmd());
            UIScreen.RemoveDialog(this);
        }

        private void ForceDC()
        {
            UIScreen.RemoveDialog(this);
            GameFacade.Controller.Disconnect(false);
        }
    }
}
