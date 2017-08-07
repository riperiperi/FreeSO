/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.IO;
using System.Threading;
using FSO.Client.Utils.GameLocator;
using FSO.Client.Utils;
using System.Reflection;
using FSO.Common;
using FSO.Client.Debug;
using System.Windows.Forms;
using FSO.Common.Rendering.Framework.IO;
using FSO.UI;

namespace FSO.Client
{

    public static class Program
    {

        public static bool UseDX = true;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static void Main(string[] args)
        {
            if ((new FSOProgram()).InitWithArguments(args))
                (new GameStartProxy()).Start(UseDX);
        }
    }
}
