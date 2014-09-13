/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TSOClient
{
    /// <summary>
    /// Cache for storing letters and IMs received by other players.
    /// </summary>
    public class MessagesCache
    {
        private static string CacheDir = GlobalSettings.Default.DocumentsPath + 
            "MessageCache\\" + PlayerAccount.Username;

        /// <summary>
        /// Caches a letter received from a player.
        /// </summary>
        /// <param name="From">Player the letter was received from.</param>
        /// <param name="Subject">Subject of the letter.</param>
        /// <param name="Message">Content.</param>
        public static void CacheLetter(string From, string Subject, string Message)
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);

            using (BinaryWriter Writer = new BinaryWriter(File.Create(CacheDir + "\\" + 
                From + ", " + Subject + ".txt")))
            {
                Writer.Write(Message);
                Writer.Flush();
                Writer.Close();
            }
        }
    }
}
