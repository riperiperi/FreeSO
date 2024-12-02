using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.LotView.Components.Model
{
    internal struct SM64ObjectGeometry4C
    {
        public Vector3 Tl;
        public Vector3 Tr;
        public Vector3 Br;
        public Vector3 Bl;

        public SM64ObjectGeometry4C(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl)
        {
            var p5 = new Vector3(0.5f, 0, 0.5f);
            Tl = tl - p5;
            Tr = tr - p5;
            Br = br - p5;
            Bl = bl - p5;
        }
    }

    internal class SM64ObjectGeometryObj
    {
        public SM64ObjectGeometry4C[] Boxes;

        public SM64ObjectGeometryObj(SM64ObjectGeometry4C[] boxes)
        {
            Boxes = boxes;
        }
    }

    internal class SM64ObjectGeometry
    {
        private static SM64ObjectGeometryObj NonSolid = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] { 
                
            }
        );

        private static SM64ObjectGeometryObj GenStairsGeo(float baseHeight)
        {
            int steps = 6;
            float stepSize = 1f / steps;

            var entries = new SM64ObjectGeometry4C[steps];

            for (int i = 0; i < steps; i++)
            {
                float step = i * stepSize;
                float step1 = (i + 1) * stepSize;
                float s0 = (baseHeight + 0.25f * step1) * 2.95f;
                entries[i] = new SM64ObjectGeometry4C(new Vector3(0, s0, 1f - step), new Vector3(1, s0, 1f - step), new Vector3(1, s0, 1 - step1), new Vector3(0, s0, 1 - step1));
            }

            return new SM64ObjectGeometryObj(entries);
        }

        private static SM64ObjectGeometryObj StairLowAdv = GenStairsGeo(0f);
        private static SM64ObjectGeometryObj StairMiddleLowAdv = GenStairsGeo(0.25f);
        private static SM64ObjectGeometryObj StairMiddleHiAdv = GenStairsGeo(0.5f);
        private static SM64ObjectGeometryObj StairHiAdv = GenStairsGeo(0.75f);

        private static SM64ObjectGeometryObj StairLow = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] { 
                new SM64ObjectGeometry4C(new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 2.95f * 0.25f, 0), new Vector3(0, 2.95f * 0.25f, 0))
            }
        );

        private static SM64ObjectGeometryObj StairMiddleLow = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] {
                new SM64ObjectGeometry4C(new Vector3(0, 2.95f * 0.25f, 1), new Vector3(1, 2.95f * 0.25f, 1), new Vector3(1, 2.95f * 0.5f, 0), new Vector3(0, 2.95f * 0.5f, 0))
            }
        );

        private static SM64ObjectGeometryObj StairMiddleHi = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] {
                new SM64ObjectGeometry4C(new Vector3(0, 2.95f * 0.5f, 1), new Vector3(1, 2.95f * 0.5f, 1), new Vector3(1, 2.95f * 0.75f, 0), new Vector3(0, 2.95f * 0.75f, 0))
            }
        );

        private static SM64ObjectGeometryObj StairHi = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] {
                new SM64ObjectGeometry4C(new Vector3(0, 2.95f * 0.75f, 1), new Vector3(1, 2.95f * 0.75f, 1), new Vector3(1, 2.95f, 0), new Vector3(0, 2.95f, 0))
            }
        );

        private static SM64ObjectGeometryObj Arch = new SM64ObjectGeometryObj(
            new SM64ObjectGeometry4C[] {
                new SM64ObjectGeometry4C(new Vector3(0.25f, 2.95f, 0.75f), new Vector3(0.75f, 2.95f, 0.75f), new Vector3(0.75f, 2.95f, 0.25f), new Vector3(0.25f, 2.95f, 0.25f))
            }
        );

        private Dictionary<uint, SM64ObjectGeometryObj> GUIDToGeometry = new Dictionary<uint, SM64ObjectGeometryObj>()
        {
            { 0x8C2C13C8, NonSolid }, //Stair - Hi Pad" />
            { 0x9431BD2A, NonSolid }, //Stair - Hi stub 1" />
            { 0xB1074912, StairHi }, //Stair - Hi" />
            { 0x788A490C, StairMiddleHi }, //Stair - Middle Hi" />
            { 0xB7F590C4, NonSolid }, //Stair - Hi stub 2" />
            { 0xCFB449F7, StairMiddleLow }, //Stair - Middle Low" />
            { 0xD3EC8978, NonSolid }, //Stair - Hi stub 3" />
            { 0xFEDF4AE5, StairLow },  //Stair - Low" />
            { 0x92F1647E, NonSolid }, //Stair - Low Pad" />

            { 0xD814CFAA, NonSolid }, //Stair - Hi Pad Dark" />
            { 0xE08DD45F, NonSolid }, //Stair - Hi stub 1 Dark" />
            { 0x30CDF96E, StairHi }, //Stair - Hi Dark" />
            { 0x620CBE6E, StairMiddleHi }, //Stair - Middle Hi Dark" />
            { 0x881C8A3D, NonSolid }, //Stair - Hi stub 2 Dark" />
            { 0x01E9EEEB, StairMiddleLow }, //Stair - Middle Low Dark" />
            { 0x90C49B89, NonSolid }, //Stair - Hi stub 3 Dark" />
            { 0xBC86EC3F, StairLow },  //Stair - Low Dark" />
            { 0x580DC153, NonSolid }, //Stair - Low Pad Dark" />

            { 0x2516B451, NonSolid }, //Stair - Hi Pad Light" />
            { 0x3513AE51, NonSolid }, //Stair - Hi stub 1 Light" />
            { 0x137A8297, StairHi }, //Stair - Hi Light" />
            { 0x97E9C45B, StairMiddleHi }, //Stair - Middle Hi Light" />
            { 0xD802F02C, NonSolid }, //Stair - Hi stub 2 Light" />
            { 0xB47794FD, StairMiddleLow }, //Stair - Middle Low Light" />
            { 0xE9F0E199, NonSolid }, //Stair - Hi stub 3 Light" />
            { 0xF0DA960E, StairLow },  //Stair - Low Light" />
            { 0x82E5BB6F, NonSolid }, //Stair - Low Pad Light" />

            { 0x821EB6EE, NonSolid }, //Stair - Hi Pad Medium" />
            { 0x9470AD19, NonSolid }, //Stair - Hi stub 1 Medium" />
            { 0xFE788051, StairHi }, //Stair - Hi Medium" />
            { 0x0D90C722, StairMiddleHi }, //Stair - Middle Hi Medium" />
            { 0xB8D0F364, NonSolid }, //Stair - Hi stub 2 Medium" />
            { 0x11B197B4, StairMiddleLow }, //Stair - Middle Low Medium" />
            { 0x4184E2C7, NonSolid }, //Stair - Hi stub 3 Medium" />
            { 0x565E9547, StairLow },  //Stair - Low Medium" />
            { 0x79E2B825, NonSolid }, //Stair - Low Pad Medium" />

            //---

            { 0x8497EF34, NonSolid }, //Stair - Retro - Hi Pad" />
            { 0xA2D0F4B8, StairHi }, //Stair - Retro - Hi" />
            { 0xDBD1B0BC, NonSolid }, //Stair - Retro - Hi stub 1" />
            { 0x3A328157, NonSolid }, //Stair - Retro - Hi stub 2" />
            { 0xB6E4A535, StairMiddleHi }, //Stair - Retro - Middle Hi" />
            { 0x13BF9D7E, NonSolid }, //Stair - Retro - Hi stub 3" />
            { 0x8A15D4D3, StairMiddleLow }, //Stair - Retro - Middle Low" />
            { 0x69DFE851, StairLow },  //Stair - Retro - Low" />
            { 0x4945910C, NonSolid }, //Stair - Retro - Low Pad" />

            { 0xEAF272E1, NonSolid }, //Stair - Stone - Hi Pad" />
            { 0xE9100959, NonSolid }, //Stair - Stone - Hi stub 1" />
            { 0xEAEE45E1, StairHi }, //Stair - Stone - Hi" />
            { 0xE98E227C, StairMiddleHi }, //Stair - Stone - Middle Hi" />
            { 0xE9244080, NonSolid }, //Stair - Stone - Hi stub 2" />
            { 0xE99255F1, StairMiddleLow }, //Stair - Stone - Middle Low" />
            { 0xE9487642, NonSolid }, //Stair - Stone - Hi stub 3" />
            { 0xE95C6DBE, StairLow },  //Stair - Stone - Low" />
            { 0xE97A33C6, NonSolid }, //Stair - Stone - Low Pad" />

            //---

            { 0x9612CDF2, NonSolid }, //Stair - Bamboo - Hi Pad" />
            { 0x9613D60E, NonSolid }, //Stair - Bamboo - Hi stub 1" />
            { 0x9611FB30, StairHi }, //Stair - Bamboo - Hi" />
            { 0x966BBC1E, StairMiddleHi }, //Stair - Bamboo - Middle Hi" />
            { 0x966E8876, NonSolid }, //Stair - Bamboo - Hi stub 2" />
            { 0x9664EC83, StairMiddleLow }, //Stair - Bamboo - Middle Low" />
            { 0x966F99CC, NonSolid }, //Stair - Bamboo - Hi stub 3" />
            { 0x9668EE41, StairLow },  //Stair - Bamboo - Low" />
            { 0x966AC324, NonSolid }, //Stair - Bamboo - Low Pad" />

            { 0x604A8732, NonSolid }, //Stair - Metal - Hi Pad" />
            { 0x30CDC07E, StairHi }, //Stair - Metal - Hi" />
            { 0x97A4D6DC, NonSolid }, //Stair - Metal - Hi stub 1" />
            { 0x50B69689, NonSolid }, //Stair - Metal - Hi stub 2" />
            { 0xE030EB57, StairMiddleHi }, //Stair - Metal - Middle Hi" />
            { 0xBA04FB3E, NonSolid }, //Stair - Metal - Hi stub 3" />
            { 0xCDBF8B73, StairMiddleLow }, //Stair - Metal - Middle Low" />
            { 0xA266BC4A, StairLow },  //Stair - Metal - Low" />
            { 0x959CA0ED, NonSolid }, //Stair - Metal - Low Pad" />

            // ---

            { 0x095B9241, NonSolid }, //Stair - Beach - Hi Pad" />
            { 0x0EBDCC39, NonSolid }, //Stair - Beach - Hi stub 1" />
            { 0x097989A2, StairHi }, //Stair - Beach - Hi" />
            { 0x0E17A8CC, StairMiddleHi }, //Stair - Beach - Middle Hi" />
            { 0x0E9FDD83, NonSolid }, //Stair - Beach - Hi stub 2" />
            { 0x0E09E0C6, StairMiddleLow }, //Stair - Beach - Middle Low" />
            { 0x0E8BAA0E, NonSolid }, //Stair - Beach - Hi stub 3" />
            { 0x0ED9876B, StairLow },  //Stair - Beach - Low" />
            { 0x0E3BF851, NonSolid }, //Stair - Beach - Low Pad" />

            { 0x09C9F6B9, NonSolid }, //Stair - Knotty Lodge - Hi Pad" />
            { 0x0907BF60, StairHi }, //Stair - Knotty Lodge - Hi" />
            { 0x09D78D01, NonSolid }, //Stair - Knotty Lodge - Hi stub 1" />
            { 0x09F5BA01, NonSolid }, //Stair - Knotty Lodge - Hi stub 2" />
            { 0x0877EB43, StairMiddleHi }, //Stair - Knotty Lodge - Middle Hi" />
            { 0x0993A14B, NonSolid }, //Stair - Knotty Lodge - Hi stub 3" />
            { 0x0815CE56, StairMiddleLow }, //Stair - Knotty Lodge - Middle Low" />
            { 0x09B1C619, StairLow },  //Stair - Knotty Lodge - Low" />
            { 0x0869E11B, NonSolid }, //Stair - Knotty Lodge - Low Pad" />

            // ---

            { 0x38D0DF9E, Arch }, //Pluto's Arch" />
            { 0x3B8937E4, Arch }, //Archum Aqueductum" />
            { 0x3A2737BA, Arch }, //Moderne Deko Arch" />
            { 0x3A99379B, Arch }, //Promotional Balloon Column" />
            { 0x218E9FCF, Arch }, //Moroccan Column Arch" />
            { 0x345E3754, Arch }, //Numantian Column" />
            { 0x2913A1E9, Arch }, //Cobalt-60 Column" />
            { 0x259AACA5, Arch }, //Castle Keystonne Arched Columns" />
            { 0x27B7D0B0, Arch }, //Arch de la Nuit" />
            { 0x266F98BA, Arch }, //Arch Zilbert" />
        };

        public SM64ObjectGeometryObj GetByGUID(uint guid)
        {
            if (GUIDToGeometry.TryGetValue(guid, out var result))
            {
                return result;
            }

            return null;
        }
    }
}
