/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TSO_CityServer
{
    /// <summary>
    /// A logging system accessible to all classes.
    /// </summary>
    class Logger
    {
        private static BinaryWriter m_Writer;

        public static bool DebugEnabled = false;
        public static bool WarnEnabled = false;
        public static bool InfoEnabled = false;

        /// <summary>
        /// Initializes the logging system.
        /// </summary>
        /// <param name="Path">The path and filename of the log file.</param>
        public static void Initialize(string Path)
        {
            m_Writer = new BinaryWriter(File.Open(Path, FileMode.OpenOrCreate));
        }

        /// <summary>
        /// Logs a debug message to the log.
        /// </summary>
        /// <param name="Message">The message to log.</param>
        public static void LogDebug(string Message)
        {
            if (DebugEnabled)
            {
                m_Writer.Write(Message);
                m_Writer.Flush();
            }
        }

        /// <summary>
        /// Logs a warning message to the log.
        /// </summary>
        /// <param name="Message">The message to log.</param>
        public static void LogWarning(string Message)
        {
            if (WarnEnabled)
            {
                m_Writer.Write(Message);
                m_Writer.Flush();
            }
        }

        /// <summary>
        /// Logs a info message to the log.
        /// </summary>
        /// <param name="Message">The message to log.</param>
        public static void LogInfo(string Message)
        {
            if (InfoEnabled)
            {
                m_Writer.Write(Message);
                m_Writer.Flush();
            }
        }
    }
}
