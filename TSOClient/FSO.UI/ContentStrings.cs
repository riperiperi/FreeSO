using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Client.GameContent
{
    public class ContentStrings
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> StringTable;
        public static bool TS1;

        public ContentStrings()
        {
            StringTable = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            var langdir = GlobalSettings.Default.CurrentLang.ToLowerInvariant() + ".dir";

            if (TS1)
            {
                LoadTS1();
            }
            else
            {
                var tsodir = Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata/uitext/");

                if (Directory.Exists(Path.Combine(tsodir, langdir)))
                    Load("UIText", Path.Combine(tsodir, langdir));
                else
                    Load("UIText", Path.Combine(tsodir, "english.dir"));
            }
            var fsodir = "Content/UI/uitext/";
            if (Directory.Exists(Path.Combine(fsodir, langdir)))
                Load("UIText", Path.Combine(fsodir, langdir));
            else
                Load("UIText", Path.Combine(fsodir, "english.dir"));
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
            return GetString("UIText", table, id) ?? "";
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
                    var c = ArgsStr[i];
                    if (c == '%' && i < ArgsStr.Length-1 && (ArgsStr[i+1] == 's' || ArgsStr[i+1] == 'd'))
                    {
                        if (ArgsCounter < args.Length)
                        {
                            SBuilder.Append(args[ArgsCounter]);
                            ArgsCounter++;
                            i++; //skip the argument specifier
                        }
                    }
                    else
                        SBuilder.Append(c);
                }

                return SBuilder.ToString();
            }

            return "";
        }

        public void LoadTS1()
        {
            var path = Path.Combine(Content.Content.TS1HybridBasePath, "GameData/UIText.iff");
            var iff = new IffFile(path);

            var dirName = "UIText";
            Dictionary<string, Dictionary<string, string>> table;
            if (!StringTable.TryGetValue(dirName, out table))
            {
                table = new Dictionary<string, Dictionary<string, string>>();
                StringTable.Add(dirName, table);
            }

            var tables = iff.List<STR>();
            foreach (var str in tables)
            {
                var tableData = new Dictionary<string, string>();
                for (int i=0; i<str.Length; i++)
                {
                    tableData[i.ToString()] = str.GetString(i); 
                }
                table[str.ChunkID.ToString()] = tableData; //overwrites previous.
            }
        }

        /// <summary>
        /// Loads all string tables from a specified directory.
        /// </summary>
        /// <param name="dirName">Name of directory containing string tables.</param>
        /// <param name="basePath">Base path of directory.</param>
        public void Load(string dirName, string basePath)
        {
            Dictionary<string, Dictionary<string, string>> table;
            if (!StringTable.TryGetValue(dirName, out table))
            {
                table = new Dictionary<string, Dictionary<string, string>>();
                StringTable.Add(dirName, table);
            }

            var files = Directory.GetFiles(basePath);
            foreach (var file in files)
            {
                var tableID = Path.GetFileName(file);
                var second_ = tableID.IndexOf("_", 1);
                if (second_ == -1) return;

                tableID = tableID.Substring(1, second_ - 1);

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

                table[tableID] = tableData; //overwrites previous.
            }
        }
    }
}
