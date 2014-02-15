/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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
