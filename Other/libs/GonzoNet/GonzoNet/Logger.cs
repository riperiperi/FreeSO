/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace GonzoNet
{
    public delegate void MessageLoggedDelegate(LogMessage Msg);

    public enum LogLevel
	{
		error=1
		,warn
		,info
		,verbose
	}

    public class LogMessage
    {
        public LogMessage(string Msg, LogLevel Lvl)
        {
            Message = Msg;
            Level = Lvl;
        }

        public string Message;
        public LogLevel Level;
    }

    /// <summary>
    /// A class for subscribing to messages logged by GonzoNet.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Subscribe to this event to receive messages logged by GonzoNet.
        /// </summary>
        public static event MessageLoggedDelegate OnMessageLogged;

        /// <summary>
        /// Called by classes in GonzoNet to log a message.
        /// </summary>
        /// <param name="Msg"></param>
        public static void Log(string Message, LogLevel Lvl)
        {
			
        }
    }
}
