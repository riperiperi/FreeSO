using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Mr.Shipper
{
    //IDs for files that are not in archives, taken from packingslips.txt.
    //This is a hack to avoid having to generate unique IDs (which has a tendency
    //to cause collisions.)
    public enum RogueFileIDs : uint
    {
        //uigraphics\chat
        balloonpointersadbottom = 0x1af0,
        balloonpointersadside = 0x1b00,
        balloontilessad = 0x1b10,
        //uigraphics\customcontent
        cc_bodiesbtn = 0xbff0,
        cc_dropdown = 0xc000,
        cc_dropdownbtn = 0xc010,
        cc_dropdowntextline = 0xc020,
        cc_floorsbtn = 0xc030,
        cc_headsbtn = 0xc040,
        cc_objectsbtn = 0xc050,
        cc_textentry = 0xc060,
        cc_wallsbtn = 0xc070,
        //uigraphics\friendshipweb
        f_webbaremptygreen = 0x1c20,
        f_webbaremptyred = 0x1c30,
        f_webbarfilledgreen = 0x1c40,
        f_webbarfilledred = 0x1c50,
        f_webcenter_in = 0x1c60,
        f_webcenter_out = 0x1c70,
        f_webinnerframebluegrey = 0x1c80,
        f_webouterframebluegrey = 0x1c90,
        f_webcontrolback2 = 0x1ca0,
        f_webcenterframebluegrey = 0x1cb0,
        f_webcenterframegrey = 0x1cc0,
        f_webcenterframered = 0x1cd0,
        f_webchat_l = 0x1ce0,
        f_webchat_m = 0x1cf0,
        f_webchat_s = 0x1d00,
        pp_relationshipbargreen = 0x1d10,
        pp_relationshipbarred = 0x1d20,
        f_webbaremptygreen_c = 0x1d30,
        f_webbaremptygreen_l = 0x1d40,
        f_webbaremptygreen_r = 0x1d50,
        f_webbaremptyred_c = 0x1d60,
        f_webbaremptyred_l = 0x1d70,
        f_webbaremptyred_r = 0x1d80,
        f_webbarfilledgreen_c = 0x1d90,
        f_webbarfilledgreen_l = 0x1da0,
        f_webbarfilledgreen_r = 0x1db0,
        f_webbarfilledred_c = 0x1dc0,
        f_webbarfilledred_l = 0x1dd0,
        f_webbarfilledred_r = 0x1de0,
        f_webcenterframeblack = 0x1eb0,
        f_webinnerframeblack = 0x1ec0,
        f_webouterframeblack = 0x1ed0,
        f_webcontrolblack3 = 0x1ee0,
        //uigraphics\hints
        hint1 = 0xb430,
        hint2 = 0xb440,
        hint3 = 0xb450,
        hint4 = 0xb460,
        hint5 = 0xb470,
        hint6 = 0xb480,
        hint7 = 0xb490,
        hint8 = 0xb4a0,
        hint_buildmode = 0xb4b0,
        hint_buildtools = 0xb4c0,
        hint_fweb = 0xb4d0,
        hint_leaveproperty = 0xb4e0,
        hint_moneyfilter = 0xb4f0,
        hint_motives = 0xb500,
        hint_nearzoom = 0xb510,
        hint_pip = 0xb520,
        hint_search = 0xb530,
        hint_simpage = 0xb540,
        hint_skillgain = 0xb550,
        hint_solojob1 = 0xb560,
        hint_visitors = 0xb570,
        momi_iconframe = 0xb580,
        //uigraphics\holiday
        setup_halloween = 0xcdd0,
        setup_thanksgiving = 0xcde0,
        setup_valentine = 0x1000,
        setup_xmas = 0x2000,
        setup_paddys_day = 0x4000
    }

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
