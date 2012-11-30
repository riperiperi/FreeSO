using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Mr.Shipper
{
    class Database
    {
        private static Filter<ulong> AccessoryFilter, AnimFilter;

        public static void BuildEntryDatabase()
        {
            XmlDataDocument AccessoryTable = new XmlDataDocument();
            AccessoryTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\accessorytable.xml");

            XmlNodeList NodeList = AccessoryTable.GetElementsByTagName("DefineAssetString");
            AccessoryFilter = new Filter<ulong>(NodeList.Count, 0.01f, delegate(ulong Input) { return 0; });

            foreach (XmlNode Node in NodeList)
            {
                ulong ID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                AccessoryFilter.Add(ID);
            }

            XmlDataDocument AnimTable = new XmlDataDocument();
            AnimTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\animtable.xml");

            NodeList = AnimTable.GetElementsByTagName("DefineAssetString");
            AnimFilter = new Filter<ulong>(NodeList.Count, 0.01f, delegate(ulong Input) { return 0; });

            foreach (XmlNode Node in NodeList)
            {
                ulong ID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                AnimFilter.Add(ID);
            }
        }

        /// <summary>
        /// Checks if a supplied ID collides with a FileID in the DB.
        /// </summary>
        /// <param name="ID">The ID to query for.</param>
        /// <returns>Returns true if the supplied FileID exists in the entry database, false otherwise.</returns>
        public static bool CheckIDCollision(ulong ID)
        {
            bool AccessoryTableCollision = false, AnimTableCollision = false;

            if (AccessoryFilter.Contains(ID))
            {
                Console.WriteLine("Found ID collision in AccessoryTable: " + string.Format("{0:X}", ID));
                AccessoryTableCollision = true;
            }

            if (AnimFilter.Contains(ID))
            {
                Console.WriteLine("Found ID collision in AnimationTable: " + string.Format("{0:X}", ID));
                AnimTableCollision = true;
            }

            if(AnimTableCollision || AccessoryTableCollision)
                return true;

            return false;
        }
    }
}
