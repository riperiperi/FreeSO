/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (GlobalSettings.Default.DEBUG_BUILD)
            {
                LogMessage Msg = new LogMessage(Message, Lvl);
                OnMessageLogged(Msg);
            }
        }
    }
}
