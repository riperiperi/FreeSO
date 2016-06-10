/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.Client.GameContent
{
    public class ContentStrings
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> StringTable;

        public ContentStrings()
        {
            StringTable = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            if (Directory.Exists(Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata/uitext/" + GlobalSettings.Default.CurrentLang.ToLowerInvariant() + ".dir")))
                Load("UIText", Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata/uitext/" + GlobalSettings.Default.CurrentLang.ToLowerInvariant() + ".dir"));
            else
                Load("UIText", Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata/uitext/english.dir"));
        }

        public string this[string dir, string table, string id]
        {
            get
            {
                return GetString(dir, table, id);
            }
        }

        /// <summary>
        /// Gets a string in the specified table with the specified ID.
        /// </summary>
        /// <param name="table">Name of string table (*.cst).</param>
        /// <param name="id">ID of string.</param>
        /// <returns>The retrieved string, or null if not found.</returns>
        public string GetString(string table, string id)
        {
            return GetString("UIText", table, id);
        }

        /// <summary>
        /// Gets a string from the specified directory in the specified table with the specified ID.
        /// </summary>
        /// <param name="dir">Directory containing string table.</param>
        /// <param name="table">Name of string table (*.cst).</param>
        /// <param name="id">ID of string.</param>
        /// <returns>The retrieved string, or null if not found.</returns>
        public string GetString(string dir, string table, string id)
        {
            string value = "***MISSING***";
            if (StringTable.ContainsKey(dir))
            {
                if (StringTable[dir].ContainsKey(table))
                {
                    StringTable[dir][table].TryGetValue(id, out value);
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a string from the specified table with the specified ID, and replaces %d and %s with the
        /// arguments passed.
        /// </summary>
        /// <param name="table">Table to retrieve string from.</param>
        /// <param name="id">ID of string.</param>
        /// <param name="args">Args with which to replace %d and %s in the retrieved string.</param>
        /// <returns>The retrieved string containing the arguments passed.</returns>
        public string GetString(string table, string id, string[] args)
        {
            string ArgsStr = GetString("UIText", table, id);

            if (ArgsStr != null)
            {
                StringBuilder SBuilder = new StringBuilder();
                int ArgsCounter = 0;

                for (int i = 0; i < ArgsStr.Length; i++)
                {
                    string CurrentArg = ArgsStr.Substring(i, 1);

                    if (CurrentArg.Contains("%"))
                    {
                        if (ArgsCounter < args.Length)
                        {
                            SBuilder.Append(CurrentArg.Replace("%", args[ArgsCounter]));
                            ArgsCounter++;
                            i++; //Next, CurrentArg will be either s or d - skip it!
                        }
                    }
                    else
                        SBuilder.Append(CurrentArg);
                }

                return SBuilder.ToString();
            }

            return "";
        }

        /// <summary>
        /// Loads all string tables from a specified directory.
        /// </summary>
        /// <param name="dirName">Name of directory containing string tables.</param>
        /// <param name="basePath">Base path of directory.</param>
        public void Load(string dirName, string basePath)
        {
            var table = new Dictionary<string, Dictionary<string, string>>();
            StringTable.Add(dirName, table);

            var files = Directory.GetFiles(basePath);
            foreach (var file in files)
            {
                var tableID = Path.GetFileName(file);
                tableID = tableID.Substring(1, tableID.IndexOf("_", 1) - 1);

                var tableData = new Dictionary<string, string>();

                var contentLines = File.ReadAllLines(file).ToList();
                /** Expected pattern: {digit} ^ {TXT} ^ **/

                var io = 0;
                var pos = 0;
                var index = 0;

                for (int i = 0; i < contentLines.Count; i++){
                    var line = contentLines[i];
                    if (line.StartsWith("//"))
                    {
                        /** Remove comment **/
                        contentLines.RemoveAt(i);
                        i--;
                    }
                }

                var content = String.Join("\r\n", contentLines.ToArray());

                while ((pos = content.IndexOf("^", io)) != -1)
                {
                    var id = content.Substring(io, pos - io).Trim();
                    var lastLB = id.LastIndexOf("\r\n");
                    if (lastLB != -1)
                    {
                        id = id.Substring(lastLB + 2).Trim();
                    }
                    var endPOW = content.IndexOf("^", pos + 1);
                    if (endPOW == -1) { break; }

                    pos++;
                    var strValue = content.Substring(pos, endPOW - pos);
                    io = endPOW + 1;
                    if (id.Length == 0)
                    {
                        id = index.ToString();
                    }

                    tableData[id] = strValue;
                    index++;
                }

                table[tableID] = tableData;
            }
        }
    }
}
