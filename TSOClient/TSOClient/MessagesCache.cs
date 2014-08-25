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
