using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public abstract class UIEOD : UIContainer
    {
        public Dictionary<string, EODDirectPlaintextEventHandler> PlaintextHandlers;
        public Dictionary<string, EODDirectBinaryEventHandler> BinaryHandlers;

        public UIEODController Controller;

        public UIEOD(UIEODController controller)
        {
            PlaintextHandlers = new Dictionary<string, EODDirectPlaintextEventHandler>();
            BinaryHandlers = new Dictionary<string, EODDirectBinaryEventHandler>();
            Controller = controller;
        }

        public UILotControl LotController
        {
            get
            {
                var screen = UIScreen.Current as CoreGameScreen;
                if (screen == null){
                    return null;
                }

                return screen.LotControl;
            }
        }

        public virtual void OnExpand()
        {
        }

        public virtual void OnContract()
        {
        }

        public virtual void OnClose()
        {
            Controller.CloseEOD();
        }

        public void SetTip(string txt)
        {
            Controller.EODMessage = txt;
        }

        public void SetTime(int time)
        {
            Controller.EODTime = " "+((time<0)?"":((time/60)+":"+((time%60).ToString().PadLeft(2, '0'))));
        }

        public void Send(string evt, string data)
        {
            Controller.Lot.vm.SendCommand(new VMNetEODMessageCmd
            {
                PluginID = Controller.ActivePID,
                EventName = evt,
                Binary = false,
                TextData = data
            });
        }

        public void Send(string evt, byte[] data)
        {
            Controller.Lot.vm.SendCommand(new VMNetEODMessageCmd
            {
                PluginID = Controller.ActivePID,
                EventName = evt,
                Binary = true,
                BinData = data
            });
        }
    }
}
