using System.Linq;
using System.IO;
using System.Xml;
using FSO.Files.Formats.IFF;

namespace FSO.Files.Formats.OTF
{
    /// <summary>
    /// Object Tuning File (OTF) is an SGML format which defines tuning constants.
    /// </summary>
    public class OTFFile
    {
        public XmlDocument Document;

        /// <summary>
        /// Constructs an OTF instance from a filepath.
        /// </summary>
        /// <param name="filepath">Path to the OTF.</param>
        public OTFFile(string filepath)
        {
            using (var stream = File.OpenRead(filepath))
            {
                this.Read(stream);
            }
        }

        public OTFFile()
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
            return Tables.FirstOrDefault(x => x?.ID == ID);
        }

        /// <summary>
        /// Reads an OTF from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {
            var doc = new XmlDocument();
            doc.Load(stream);

            if (IffFile.RETAIN_CHUNK_DATA) Document = doc;

            var tables = doc.GetElementsByTagName("T");
            Tables = new OTFTable[tables.Count];

            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables.Item(i);
                if (table.NodeType == XmlNodeType.Comment) continue;
                var tableEntry = new OTFTable();
                tableEntry.ID = int.Parse(table.Attributes["i"].Value);
                tableEntry.Name = table.Attributes["n"].Value;

                var numKeys = table.ChildNodes.Count;
                tableEntry.Keys = new OTFTableKey[numKeys];

                for (var x = 0; x < numKeys; x++)
                {
                    var key = table.ChildNodes[x];

                    if (key.NodeType == XmlNodeType.Comment) continue;

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

    public class OTFTable
    {
        public int ID;
        public string Name;
        public OTFTableKey[] Keys;

        public OTFTableKey GetKey(int id)
        {
            return Keys.FirstOrDefault(x => x?.ID == id);
        }
    }

    public class OTFTableKey
    {
        public int ID;
        public string Label;
        public int Value;
    }
}
