/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
