using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSOClient
{
    public class ContentStrings
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> StringTable;

        public ContentStrings()
        {
            StringTable = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            Load("UIText", Path.Combine(GlobalSettings.Default.StartupPath, @"gamedata\uitext\english.dir"));
        }


        public string this[string dir, string table, string id]
        {
            get
            {
                return GetString(dir, table, id);
            }
        }

        public string GetString(string table, string id)
        {
            return GetString("UIText", table, id);
        }

        public string GetString(string dir, string table, string id)
        {
            if (StringTable.ContainsKey(dir))
            {
                if (StringTable[dir].ContainsKey(table))
                {
                    return StringTable[dir][table][id];
                }
            }
            return null;
        }

        public void Load(string dirName, string basePath)
        {
            var table = new Dictionary<string, Dictionary<string, string>>();
            StringTable.Add(dirName, table);

            var files = Directory.GetFiles(basePath);
            foreach (var file in files)
            {
                var tableID = Path.GetFileName(file);
                tableID = tableID.Substring(1, tableID.IndexOf("_", 1) - 1);
                if (tableID == "155")
                {
                    var x = true;
                }

                var tableData = new Dictionary<string, string>();

                var contentLines = File.ReadAllLines(file).ToList();
                /** Expected pattern: {digit} ^ {TXT} ^ **/

                var io = 0;
                var pos = 0;
                var index = 0;


                /** Remove comments **/
                //while ((pos = content.IndexOf("//", io)) != -1)
                //{
                //    var ends = content.IndexOf("\r\n", io);
                //    /** Only remove it if its at the start of a line **/
                //    if (pos == 0 || content[pos - 1] == '\n')
                //    {
                //        content = content.Substring(0, io) + content.Substring(ends + 2);
                //    }
                //    else
                //    {
                //        io = ends;
                //    }
                //}
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
