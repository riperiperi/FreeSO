/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Charvatia.Properties;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Charvatia
{
    public class CVMInstance
    {
        public VM state;
        private Stopwatch timeKeeper;
        private int TicksSinceSave;
        private static int SaveTickFreq = 60 * 60; //save every minute for safety
        private int Port;

        public CVMInstance(int port)
        {
            VM.UseWorld = false;
            Port = port;
            ResetVM();
        }

        public void ResetVM()
        {
            VMNetDriver driver;
            driver = new VMServerDriver(Port);

            var vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();

            vm.OnChatEvent += Vm_OnChatEvent;

            vm.SendCommand(new VMBlueprintRestoreCmd
            {
                XMLData = File.ReadAllBytes(Path.Combine(Settings.Default.GamePath + "housedata/blueprints/" + Settings.Default.DebugLot))
            });
            vm.Context.Clock.Hours = 10;
            vm.MyUID = uint.MaxValue-1;
            vm.SendCommand(new VMNetSimJoinCmd
            {
                ActorUID = uint.MaxValue - 1,
                Name = "server"
            });

            state = vm;

        }

        private void Vm_OnChatEvent(FSO.SimAntics.NetPlay.Model.VMChatEvent evt)
        {
            var print = "";
            switch (evt.Type)
            {
                case VMChatEventType.Message:
                    print = "<%> says: ".Replace("%", evt.Text[0]) + evt.Text[1]; break;
                case VMChatEventType.MessageMe:
                    print = "You say: " + evt.Text[1]; break;
                case VMChatEventType.Join:
                    print = "<%> has entered the property.".Replace("%", evt.Text[0]); break;
                case VMChatEventType.Leave:
                    print = "<%> has left the property.".Replace("%", evt.Text[0]); break;
                case VMChatEventType.Arch:
                    print = "<" + evt.Text[0] + " (" + evt.Text[1] + ")" + "> " + evt.Text[2]; break;
                case VMChatEventType.Generic:
                    print = evt.Text[0]; break;
            }

            Console.WriteLine(print);
        }

        public void SendMessage(string msg)
        {
            state.SendCommand(new VMNetChatCmd
            {
                ActorUID = uint.MaxValue - 1,
                Message = msg
            });
        }

        public void Start()
        {
            Thread oThread = new Thread(new ThreadStart(TickVM));
            oThread.Start();
        }

        private void TickVM()
        {
            timeKeeper = new Stopwatch();
            timeKeeper.Start();
            long lastMs = 0;
            while (true)
            {
                lastMs += 16;
                TicksSinceSave++;
                try {
                    state.Update();
                } catch (Exception e)
                {
                    state.CloseNet();
                    Console.WriteLine(e.ToString());
                    var exporter = new VMWorldExporter();
                    exporter.SaveHouse(state, Path.Combine(Settings.Default.GamePath + "housedata/blueprints/" + Settings.Default.DebugLot));
                    Thread.Sleep(500);

                    ResetVM();
                    //restart on exceptions... but print them to console
                    //just for people who like 24/7 servers.
                }

                if (TicksSinceSave > SaveTickFreq)
                {
                    //quick and dirty periodic save
                    var exporter = new VMWorldExporter();
                    exporter.SaveHouse(state, Path.Combine(Settings.Default.GamePath + "housedata/blueprints/" + Settings.Default.DebugLot));
                    TicksSinceSave = 0;
                }

                Thread.Sleep((int)Math.Max(0, (lastMs + 16)-timeKeeper.ElapsedMilliseconds));
            }
        }
    }
}
