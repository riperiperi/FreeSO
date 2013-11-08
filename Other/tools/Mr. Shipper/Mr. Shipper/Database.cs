using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Mr.Shipper
{
    //IDs for files that are not in archives, taken from packingslips.txt.
    //This is a hack to avoid having to generate unique IDs (which has a tendency
    //to cause collisions.)
    //Please note that filenames follow this pattern: ContainingDirectory_Filename
    public enum RogueFileIDs : uint
    {
        //uigraphics\chat
        chat_balloonpointersadbottom = 0x1af0,
        chat_balloonpointersadside = 0x1b00,
        chat_balloontilessad = 0x1b10,
        //uigraphics\customcontent
        customcontent_cc_bodiesbtn = 0xbff0,
        customcontent_cc_dropdown = 0xc000,
        customcontent_cc_dropdownbtn = 0xc010,
        customcontent_cc_dropdowntextline = 0xc020,
        customcontent_cc_floorsbtn = 0xc030,
        customcontent_cc_headsbtn = 0xc040,
        customcontent_cc_objectsbtn = 0xc050,
        customcontent_cc_textentry = 0xc060,
        customcontent_cc_wallsbtn = 0xc070,
        //uigraphics\friendshipweb
        friendshipweb_f_webbaremptygreen = 0x1c20,
        friendshipweb_f_webbaremptyred = 0x1c30,
        friendshipweb_f_webbarfilledgreen = 0x1c40,
        friendshipweb_f_webbarfilledred = 0x1c50,
        friendshipweb_f_webcenter_in = 0x1c60,
        friendshipweb_f_webcenter_out = 0x1c70,
        friendshipweb_f_webinnerframebluegrey = 0x1c80,
        friendshipweb_f_webouterframebluegrey = 0x1c90,
        friendshipweb_f_webcontrolback2 = 0x1ca0,
        friendshipweb_f_webcenterframebluegrey = 0x1cb0,
        friendshipweb_f_webcenterframegrey = 0x1cc0,
        friendshipweb_f_webcenterframered = 0x1cd0,
        friendshipweb_f_webchat_l = 0x1ce0,
        friendshipweb_f_webchat_m = 0x1cf0,
        friendshipweb_f_webchat_s = 0x1d00,
        friendshipweb_pp_relationshipbargreen = 0x1d10,
        friendshipweb_pp_relationshipbarred = 0x1d20,
        friendshipweb_f_webbaremptygreen_c = 0x1d30,
        friendshipweb_f_webbaremptygreen_l = 0x1d40,
        friendshipweb_f_webbaremptygreen_r = 0x1d50,
        friendshipweb_f_webbaremptyred_c = 0x1d60,
        friendshipweb_f_webbaremptyred_l = 0x1d70,
        friendshipweb_f_webbaremptyred_r = 0x1d80,
        friendshipweb_f_webbarfilledgreen_c = 0x1d90,
        friendshipweb_f_webbarfilledgreen_l = 0x1da0,
        friendshipweb_f_webbarfilledgreen_r = 0x1db0,
        friendshipweb_f_webbarfilledred_c = 0x1dc0,
        friendshipweb_f_webbarfilledred_l = 0x1dd0,
        friendshipweb_f_webbarfilledred_r = 0x1de0,
        friendshipweb_f_webcenterframeblack = 0x1eb0,
        friendshipweb_f_webinnerframeblack = 0x1ec0,
        friendshipweb_f_webouterframeblack = 0x1ed0,
        friendshipweb_f_webcontrolblack3 = 0x1ee0,
        //uigraphics\hints
        hints_hint1 = 0xb430,
        hints_hint2 = 0xb440,
        hints_hint3 = 0xb450,
        hints_hint4 = 0xb460,
        hints_hint5 = 0xb470,
        hints_hint6 = 0xb480,
        hints_hint7 = 0xb490,
        hints_hint8 = 0xb4a0,
        hints_hint_buildmode = 0xb4b0,
        hints_hint_buildtools = 0xb4c0,
        hints_hint_fweb = 0xb4d0,
        hints_hint_leaveproperty = 0xb4e0,
        hints_hint_moneyfilter = 0xb4f0,
        hints_hint_motives = 0xb500,
        hints_hint_nearzoom = 0xb510,
        hints_hint_pip = 0xb520,
        hints_hint_search = 0xb530,
        hints_hint_simpage = 0xb540,
        hints_hint_skillgain = 0xb550,
        hints_hint_solojob1 = 0xb560,
        hints_hint_visitors = 0xb570,
        hints_momi_iconframe = 0xb580,
        //uigraphics\holiday
        holiday_setup_halloween = 0xcdd0,
        holiday_setup_thanksgiving = 0xcde0,
        holiday_setup_valentine = 0x1000,
        holiday_setup_xmas = 0x2000,
        holiday_setup_paddys_day = 0x4000,
        //cities\city_0001
        city_0001_elevation = 0x8070,
        city_0001_forestdensity = 0x8080,
        city_0001_foresttype = 0x8090,
        city_0001_roadmap = 0x80a0,
        city_0001_terraintype = 0x80b0,
        city_0001_thumbnail = 0x8540,
        city_0001_vertexcolor = 0x80c0,
        //cities\city_0002
        city_0002_elevation = 0x80d0,
        city_0002_forestdensity = 0x80e0,
        city_0002_foresttype = 0x80f0,
        city_0002_roadmap = 0x8280,
        city_0002_terraintype = 0x8100,
        city_0002_thumnail = 0x8550,
        city_0002_vertexcolor = 0x8110,
        //cities\city_0003
        city_0003_elevation = 0x8120,
        city_0003_forestdensity = 0x8130,
        city_0003_foresttype = 0x8140,
        city_0003_roadmap = 0x8290,
        city_0003_terraintype = 0x8150,
        city_0003_thumbnail = 0x8560,
        city_0003_vertexcolor = 0x8160,
        //cities\city_0004
        city_0004_elevation = 0x8170,
        city_0004_forestdensity = 0x8180,
        city_0004_foresttype = 0x8190,
        city_0004_roadmap = 0x82a0,
        city_0004_terraintype = 0x81a0,
        city_0004_thumbnail = 0x8570,
        city_0004_vertexcolor = 0x81b0,
        //cities\city_0005
        city_0005_elevation = 0x9940,
        city_0005_forestdensity = 0x9950,
        city_0005_foresttype = 0x9960,
        city_0005_roadmap = 0x9970,
        city_0005_terraintype = 0x9980,
        city_0005_thumbnail = 0x9990,
        city_0005_vertexcolor = 0x99a0,
        //cities\city_0006
        city_0006_elevation = 0x99b0,
        city_0006_forestdensity = 0x99c0,
        city_0006_foresttype = 0x99d0,
        city_0006_roadmap = 0x99e0,
        city_0006_terraintype = 0x99f0,
        city_0006_thumbnail = 0x9a00,
        city_0006_vertexcolor = 0x9a10,
        //cities\city_0007
        city_0007_elevation = 0x9a20,
        city_0007_forestdensity = 0x9a30,
        city_0007_foresttype = 0x9a40,
        city_0007_roadmap = 0x9a50,
        city_0007_terraintype = 0x9a60,
        city_0007_thumbnail = 0x9a70,
        city_0007_vertexcolor = 0x9a80,
        //cities\city_0008
        city_0008_elevation = 0x9a90,
        city_0008_forestdensity = 0x9aa0,
        city_0008_foresttype = 0x9ab0,
        city_0008_roadmap = 0x82ac0,
        city_0008_terraintype = 0x9ad0,
        city_0008_thumbnail = 0x9ae0,
        city_0008_vertexcolor = 0x9af0,
        //cities\city_0009
        city_0009_elevation = 0x9b00,
        city_0009_forestdensity = 0x9b10,
        city_0009_foresttype = 0x9b20,
        city_0009_roadmap = 0x9b30,
        city_0009_terraintype = 0x9b40,
        city_0009_thumbnail = 0x9b50,
        city_0009_vertexcolor = 0x9b60,
        //cities\city_0010
        city_0010_elevation = 0x9b70,
        city_0010_forestdensity = 0x9b80,
        city_0010_foresttype = 0x9b90,
        city_0010_roadmap = 0x9ba0,
        city_0010_terraintype = 0x9bb0,
        city_0010_thumbnail = 0x9bc0,
        city_0010_vertexcolor = 0x9bd0,
        //cities\city_0011
        city_0011_elevation = 0x9be0,
        city_0011_forestdensity = 0x9bf0,
        city_0011_foresttype = 0x9c00,
        city_0011_roadmap = 0x9c100,
        city_0011_terraintype = 0x9c20,
        city_0011_thumbnail = 0x9c30,
        city_0011_vertexcolor = 0x9c40,
        //cities\city_0012
        city_0012_elevation = 0x9c50,
        city_0012_forestdensity = 0x9c60,
        city_0012_foresttype = 0x9c70,
        city_0012_roadmap = 0x9c80,
        city_0012_terraintype = 0x9c90,
        city_0012_thumnail = 0x9ca0,
        city_0012_vertexcolor = 0x9cb0,
        //cities\city_0013
        city_0013_elevation = 0x9cc0,
        city_0013_forestdensity = 0x9cd0,
        city_0013_foresttype = 0x9ce0,
        city_0013_roadmap = 0x9bf0,
        city_0013_terraintype = 0x9d0,
        city_0013_thumbnail = 0x9d10,
        city_0013_vertexcolor = 0x9d20,
        //cities\city_0014
        city_0014_elevation = 0x9d30,
        city_0014_forestdensity = 0x9d40,
        city_0014_foresttype = 0x9d50,
        city_0014_roadmap = 0x9d60,
        city_0014_terraintype = 0x9d70,
        city_0014_thumbnail = 0x9d80,
        city_0014_vertexcolor = 0x9d90,
        //cities\city_0015
        city_0015_elevation = 0x9da0,
        city_0015_forestdensity = 0x9db0,
        city_005_foresttype = 0x9dc0,
        city_0015_roadmap = 0x9dd0,
        city_0015_terraintype = 0x9de0,
        city_0015_thumbnail = 0x9df00,
        city_0015_vertexcolor = 0x9e0,
        //cities\city_0016
        city_0016_elevation = 0x9e10,
        city_0016_forestdensity = 0x9e20,
        city_0016_foresttype = 0x9e30,
        city_0016_roadmap = 0x9e40,
        city_0016_terraintype = 0x9e50,
        city_0016_thumbnail = 0x9e60,
        city_0016_vertexcolor = 0x9e70,
        //cities\city_0017
        city_0017_elevation = 0x9e80,
        city_0017_forestdensity = 0x9e90,
        city_0017_foresttype = 0x9ea0,
        city_0017_roadmap = 0x9eb0,
        city_0017_terraintype = 0x9ec0,
        city_0017_thumbnail = 0x9ed0,
        city_0017_vertexcolor = 0x9ee0,
        //cities\city_0018
        city_0018_elevation = 0x9ef00,
        city_0018_forestdensity = 0x9f0,
        city_0018_foresttype = 0x9f10,
        city_0018_roadmap = 0x9f20,
        city_0018_terraintype = 0x9f30,
        city_0018_thumbnail = 0x9f40,
        city_0018_vertexcolor = 0x9f50,
        //cities\city_0019
        city_0019_elevation = 0x9f90,
        city_0019_forestdensity = 0x9fa0,
        city_0019_foresttype = 0x9fb0,
        city_0019_roadmap = 0x9fc0,
        city_0019_terraintype = 0x9fd0,
        city_0019_thumbnail = 0x9fe0,
        city_0019_vertexcolor = 0x9ff0,
        //cities\city_0020
        city_0020_elevation = 0xad60,
        city_0020_forestdensity = 0xad80,
        city_0020_foresttype = 0xad901,
        city_0020_roadmap = 0xaf50,
        city_0020_terraintype = 0xada0,
        city_0020_thumbnail = 0xaf60,
        city_0020_vertexcolor = 0xadb0,
        //cities\city_0021
        city_0021_elevation = 0xadc0,
        city_0021_forestdensity = 0xade0,
        city_0021_foresttype = 0xadf0,
        city_0021_roadmap = 0xb040,
        city_0021_terraintype = 0xae00,
        city_0021_thumbnail = 0xaf80,
        city_0021_vertexcolor = 0xae10,
        //cities\city_0022
        city_0022_elevation = 0xae20,
        city_0022_forestdensity = 0xae40,
        city_0022_foresttype = 0xae50,
        city_0022_roadmap = 0xb050,
        city_0022_terraintype = 0xae60,
        city_0022_thumbnail = 0xaf90,
        city_0022_vertexcolor = 0xae70,
        //cities\city_0023
        city_0023_elevation = 0xae80,
        city_0023_forestdensity = 0xaea0,
        city_0023_foresttype = 0xaeb0,
        city_0023_roadmap = 0xafa0,
        city_0023_terraintype = 0xaec0,
        city_0023_thumbnail = 0xafb0,
        city_0023_vertexcolor = 0xaed0,
        //cities\city_0024
        city_0024_elevation = 0xaee0,
        city_0024_forestdensity = 0xaf0,
        city_0024_foresttype = 0xaf10,
        city_0024_roadmap = 0xafc0,
        city_0024_terraintype = 0xaf2,
        city_0024_thumbnail = 0xafd0,
        city_0024_vertexcolor = 0xaf30,
        //cities\city_0025
        city_0025_elevation = 0xb0d0,
        city_0025_forestdensity = 0xb0e0,
        city_0025_foresttype = 0xb0f0,
        city_0025_roadmap = 0xb100,
        city_0025_terraintype = 0xb110,
        city_0025_thumbnail = 0xb120,
        city_0025_vertexcolor = 0xb130,
        //cities\city_0026
        city_0026_elevation = 0xb140,
        city_0026_forestdensity = 0xb150,
        city_0026_foresttype = 0xb160,
        city_0026_roadmap = 0xb170,
        city_0026_terraintype = 0xb180,
        city_0026_thumbnail = 0xb190,
        city_0026_vertexcolor = 0xb1a0,
        //cities\city_0027
        city_0027_elevation = 0xb1b0,
        city_0027_forestdensity = 0xb1c0,
        city_0027_foresttype = 0xb1d0,
        city_0027_roadmap = 0xb1e0,
        city_0027_terraintype = 0xb1f0,
        city_0027_thumbnail = 0xb200,
        city_0027_vertexcolor = 0xb210,
        //cities\city_0028
        city_0028_elevation = 0xb220,
        city_0028_forestdensity = 0xb230,
        city_0028_foresttype = 0xb240,
        city_0028_roadmap = 0xb250,
        city_0028_terraintype = 0xb260,
        city_0028_thumbnail = 0xb270,
        city_0028_vertexcolor = 0xb280,
        //cities\city_0029
        city_0029_elevation = 0xb290,
        city_0029_forestdensity = 0xb2a0,
        city_0029_foresttype = 0xb2b0,
        city_0029_roadmap = 0xb2c0,
        city_0029_terraintype = 0xb2d0,
        city_0029_thumbnail = 0xb2e0,
        city_0029_vertexcolor = 0xb2f0,
        //cities\city_0030
        city_0030_elevation = 0xb300,
        city_0030_forestdensity = 0xb310,
        city_0030_foresttype = 0xb320,
        city_0030_roadmap = 0xb330,
        city_0030_terraintype = 0xb340,
        city_0030_thumbnail = 0xb350,
        city_0030_vertexcolor = 0xb360
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
