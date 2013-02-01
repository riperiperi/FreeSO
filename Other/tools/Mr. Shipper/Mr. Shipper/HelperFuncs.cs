/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is Mr. Shipper.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimsLib.FAR3;

namespace Mr.Shipper
{
    public class HelperFuncs
    {
        /// <summary>
        /// Returns the TypeID for a given filetype.
        /// </summary>
        /// <param name="Filetype">A filetype used by the game.</param>
        /// <returns>The TypeID for the supplied filetype.</returns>
        public static uint GetTypeID(string Filetype)
        {
            switch (Filetype)
            {
                case ".bmp":
                    return 0x856DDBAC; //Not compressed with Persist/RefPack
                case ".tga":
                    return 2;
                case ".skel":
                    return 5;
                case ".anim":
                    return 7;
                case ".mesh":
                    return 9;
                case ".bnd":
                    return 11;
                case ".apr":
                    return 12;
                case ".oft":
                    return 13;
                case ".po":
                    return 15;
                case ".col":
                    return 16;
                case ".hag":
                    return 18;
                case ".jpg":
                    return 20;
                case ".png":
                    return 24; //Not compressed with Persist/RefPack
                case ".mad":
                    return 0x0A8B0E70;
                case ".utk":
                    return 0x1B6B9806;
                case ".xa":
                    return 0x1D07EB4B;
                case ".mp3":
                    return 0x3CEC2B47;
                case ".trk":
                    return 0x5D73A611;
                case ".hit":
                    return 0x7B1ACFCD;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Returns a FileID for a rogue file (file that isn't archived).
        /// </summary>
        /// <param name="Entry">The entry generated for the file.</param>
        /// <returns>A FileID (see RogueFileIDs enum in Database.cs)</returns>
        public static uint GetFileID(Far3Entry Entry)
        {
            try
            {
                string Filename = Path.GetFileName(Entry.Filename);
                Filename = Filename.Replace("-", "_");
                Filename = Filename.Substring(0, Filename.IndexOf("."));

                return (uint)Enum.Parse(typeof(RogueFileIDs), Filename);
            }
            catch (ArgumentException)
            {
                return (uint)Entry.GetHashCode();
            }
        }

        /// <summary>
        /// Checks for collisions between existing and generated IDs, and prints out if any were found.
        /// </summary>
        /// <param name="FileID">The generated ID to check.</param>
        /// <param name="UIEntries">The entries to check.</param>
        /// <returns>True if any collisions were found.</returns>
        public static bool CheckCollision(ulong ID, Dictionary<Far3Entry, string> UIEntries)
        {
            foreach (KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                if (KVP.Key.FileID == ID)
                    Console.WriteLine("Found ID collision: " + ID);
            }

            if (Database.CheckIDCollision(ID))
                return true;

            return false;
        }

        public static ulong Get64BitRandom(ulong minValue, ulong maxValue)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);

            // Get a random array of 8 bytes. 
            // As an option, you could also use the cryptography namespace stuff to generate a random byte[8]
            byte[] buffer = new byte[sizeof(ulong)];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0) % (maxValue - minValue + 1) + minValue;
        }

        /// <summary>
        /// Sanitize's a file's name so it can be used in an C# enumeration.
        /// </summary>
        /// <param name="Filename">The name to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        public static string SanitizeFilename(string Filename)
        {
            return Filename.Replace(".bmp", "").Replace(".tga", "").
                        Replace("'", "").Replace("-", "_").Replace(".ttf", "").Replace(".wve", "").
                        Replace(".png", "").Replace(" ", "_").Replace("1024_768frame", "_1024_768frame").
                        Replace(".anim", "").Replace(".mesh", "").Replace(".skel", "").Replace(".col", "").
                        Replace(".ffn", "").Replace(".cur", "").Replace(".po", "").Replace(".oft", "").
                        Replace(".hag", "").Replace(".jpg", "").Replace(".max", "").Replace(".bnd", "").
                        Replace(".apr", "");
        }

        /// <summary>
        /// Converts a TypeID and FileID to a ulong that can be converted to a hex string
        /// for output as XML.
        /// </summary>
        /// <param name="TypeID">A TypeID from a FAR3Entry.</param>
        /// <param name="FileID">A FileID from a FAR3Entry.</param>
        /// <returns></returns>
        public static ulong ToID(uint TypeID, uint FileID)
        {
            MemoryStream MemStream = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(MemStream);

            Writer.Write(TypeID);
            Writer.Write(FileID);

            return BitConverter.ToUInt64(MemStream.ToArray(), 0);
        }
    }
}
