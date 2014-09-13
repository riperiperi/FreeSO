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
using System.Xml;

namespace TSO.Files.formats.otf
{
    /// <summary>
    /// Object Tuning File (OTF) is an SGML format which defines tuning constants.
    /// </summary>
    public class OTF
    {

        /// <summary>
        /// Constructs an OTF instance from a filepath.
        /// </summary>
        /// <param name="filepath">Path to the OTF.</param>
        public OTF(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                this.Read(stream);
            }
        }

        public OTF()
        {
            //you can also create empty OTFs!
        }

        public OTFTable[] Tables;

        /// <summary>
        /// Gets an OTFTable instance from an ID.
        /// </summary>
        /// <param name="ID">The ID of the table.</param>
        /// <returns>An OTFTable instance.</returns>
        public OTFTable GetTable(int ID)
        {
            return Tables.FirstOrDefault(x => x.ID == ID);
        }

        /// <summary>
        /// Reads an OTF from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            var tables = doc.GetElementsByTagName("T");
            Tables = new OTFTable[tables.Count];

            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables.Item(i);
                var tableEntry = new OTFTable();
                tableEntry.ID = int.Parse(table.Attributes["i"].Value);
                tableEntry.Name = table.Attributes["n"].Value;

                var numKeys = table.ChildNodes.Count;
                tableEntry.Keys = new OTFTableKey[numKeys];

                for (var x = 0; x < numKeys; x++)
                {
                    var key = table.ChildNodes[x];
                    var keyEntry = new OTFTableKey();
                    keyEntry.ID = int.Parse(key.Attributes["i"].Value);
                    keyEntry.Label = key.Attributes["l"].Value;
                    keyEntry.Value = int.Parse(key.Attributes["v"].Value);
                    tableEntry.Keys[x] = keyEntry;
                }

                Tables[i] = tableEntry;
            }
        }
    }

    //BELOW CLASSES NEEDS DOCUMENTATION - THANKS DARREN!!

    public class OTFTable
    {
        public int ID;
        public string Name;
        public OTFTableKey[] Keys;

        public OTFTableKey GetKey(int id)
        {
            return Keys.FirstOrDefault(x => x.ID == id);
        }
    }

    public class OTFTableKey
    {
        public int ID;
        public string Label;
        public int Value;
    }
}
