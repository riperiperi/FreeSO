/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Charvatia.Properties;
using GonzoNet;
using ProtocolAbstractionLibraryD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Charvatia
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading Content...");
            FSO.Content.Content.Init(Settings.Default.GamePath, null);
            Console.WriteLine("Success!");

            Console.WriteLine("Starting VM server...");

            PacketHandlers.Register((byte)PacketType.VM_PACKET, false, 0, new OnPacketReceive(VMPacket));

            StartVM();

            while (true)
                Thread.Sleep(16);
        }

        static private void VMPacket(NetworkClient client, ProcessedPacket packet)
        {

        }

        static void StartVM()
        {
            var test = new CVMInstance(37564);
            Console.WriteLine("Stunning success.");
            test.Start();
        }
    }
}
