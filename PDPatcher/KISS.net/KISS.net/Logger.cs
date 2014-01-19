using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KISS
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
