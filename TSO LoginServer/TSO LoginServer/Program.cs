/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TSO_LoginServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			//From: http://tech.pro/tutorial/668/csharp-tutorial-dealing-with-unhandled-exceptions
			//With this method hooked to the Application.ThreadException, unhandled exceptions on 
			//the main application thread will not hit the UnhandledException event on the 
			//AppDomain - and the app will no longer terminate by default. As you can see in this 
			//method, we show a dialog asking if the user wants to continue or not - and if they 
			//choose abort, we close the app. Otherwise, we just let the app continue on.
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            if(ex != null)
                Logger.LogDebug("Unhandled exception: \n" + ex.ToString()); 
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Logger.LogDebug("Unhandled exception: \n" + e.Exception.ToString()); 
        }
    }
}
