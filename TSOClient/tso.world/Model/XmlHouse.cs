﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace FSO.LotView.Model
{
    [XmlRoot("house")]
    public class XmlHouseData
    {
        [XmlElement("size")]
        public int Size { get; set; }

        [XmlElement("category")]
        public int Category { get; set; }

        [XmlElement("world")]
        public XmlHouseDataWorld World { get; set; }

        [XmlArray("objects")]
        [XmlArrayItem("object")]
        public List<XmlHouseDataObject> Objects { get; set; }

        [XmlArray("sounds")]
        [XmlArrayItem("sound")]
        public List<XmlSoundData> Sounds { get; set; }

        public static XmlHouseData Parse(string xmlFilePath)
        {
            using (var reader = File.OpenRead(xmlFilePath))
            {
                return Parse(reader);
            }
        }

        public static XmlHouseData Parse(Stream reader)
        {
            XmlSerializer serialize = new XmlSerializer(typeof(XmlHouseData));
            return (XmlHouseData)serialize.Deserialize(reader);
        }

        public static void Save(string xmlFilePath, XmlHouseData data)
        {
            XmlSerializer serialize = new XmlSerializer(typeof(XmlHouseData));

            using (var writer = new StreamWriter(xmlFilePath))
            {
                serialize.Serialize(writer, data);
            }
        }
    }

    public class XmlSoundData
    {
        [XmlAttribute("id")]
        public uint ID;

        [XmlAttribute("on")]
        public int On;
    }

    public class XmlHouseDataObject
    {
        public uint GUIDInt
        {
            get
            {
                return Convert.ToUInt32(GUID, 16);
            }
        }

        [XmlAttribute("guid")]
        public string GUID;

        [XmlAttribute("level")]
        public int Level { get; set; }

        [XmlAttribute("x")]
        public int X { get; set; }

        [XmlAttribute("y")]
        public int Y { get; set; }

        [XmlAttribute("dir")]
        public int Dir { get; set; }

        [XmlAttribute("group")]
        public int Group { get; set; }

        public Direction Direction
        {
            get
            {
                switch (Dir)
                {
                    case 6:
                        return Direction.WEST;
                    case 4:
                        return Direction.SOUTH;
                    case 2:
                        return Direction.EAST;
                    case 0:
                        return Direction.NORTH;
                }
                return Direction.WEST;
            }
        }
    }

    public class XmlHouseDataWorld
    {
        [XmlArray("floors")]
        [XmlArrayItem("floor")]
        public List<XmlHouseDataFloor> Floors;

        [XmlArray("walls")]
        [XmlArrayItem("wall")]
        public List<XmlHouseDataWall> Walls;

        [XmlArray("pools")]
        [XmlArrayItem("pool")]
        public List<XmlHouseDataPool> Pools;
    }

    public class XmlHouseDataFloor
    {
        [XmlAttribute("level")]
        public int Level { get; set; }

        [XmlAttribute("x")]
        public short X { get; set; }

        [XmlAttribute("y")]
        public short Y { get; set; }

        [XmlAttribute("value")]
        public int Value { get; set; }
    }

    public class XmlHouseDataPool
    {
        [XmlAttribute("x")]
        public short X { get; set; }

        [XmlAttribute("y")]
        public short Y { get; set; }

        [XmlAttribute("value")]
        public int Value { get; set; }
    }

    public class XmlHouseDataWall
    {
        [XmlAttribute("level")]
        public int Level { get; set; }

        [XmlAttribute("x")]
        public int X { get; set; }

        [XmlAttribute("y")]
        public int Y { get; set; }

        [XmlAttribute("segments")]
        public int _Segments
        {
            get { return (int)Segments; }
            set { Segments = (WallSegments)value; }
        }

        public WallSegments Segments { get; set; }


        [XmlAttribute("placement")]
        public int Placement { get; set; }


        [XmlAttribute("tls")]
        public int LeftStyle { get; set; }
        [XmlAttribute("trs")]
        public int RightStyle { get; set; }

        [XmlAttribute("tlp")]
        public int TopLeftPattern { get; set; }
        [XmlAttribute("trp")]
        public int TopRightPattern { get; set; }
        [XmlAttribute("brp")]
        public int BottomRightPattern { get; set; }
        [XmlAttribute("blp")]
        public int BottomLeftPattern { get; set; }
    }

    [Flags]
    public enum WallSegments
    {
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        HorizontalDiag = 16,
        VerticalDiag = 32,

        AnyDiag = HorizontalDiag | VerticalDiag,
        AnyAdj = TopLeft | TopRight | BottomLeft | BottomRight
    }
}
