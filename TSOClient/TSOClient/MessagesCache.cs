/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
