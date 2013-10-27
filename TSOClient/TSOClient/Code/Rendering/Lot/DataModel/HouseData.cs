using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace TSOClient.Code.Rendering.Lot.Model
{
    [XmlRoot("house")]
    public class HouseData
    {
        [XmlElement("size")]
        public int Size {get; set;}

        [XmlElement("category")]
        public int Category { get; set; }


        [XmlElement("world")]
        public HouseDataWorld World { get; set; }


        public static HouseData Parse(string xmlFilePath)
        {
            XmlSerializer serialize = new XmlSerializer(typeof(HouseData));

            using (var reader = File.OpenRead(xmlFilePath))
            {
                return (HouseData)serialize.Deserialize(reader);
            }
        }
    }

    public class HouseDataWorld
    {
        [XmlArray("floors")]
        [XmlArrayItem("floor")]
        public List<HouseDataFloor> Floors;

        [XmlArray("walls")]
        [XmlArrayItem("wall")]
        public List<HouseDataWall> Walls;
    }

    public class HouseDataFloor
    {
        [XmlAttribute("level")]
        public int Level { get; set; }

        [XmlAttribute("x")]
        public int X { get; set; }

        [XmlAttribute("y")]
        public int Y { get; set; }

        [XmlAttribute("value")]
        public int Value { get; set; }
    }


    public class HouseDataWall
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
        BottomLeft = 4,
        BottomRight = 8,
        HorizontalDiag = 16,
        VerticalDiag = 32
    }
}
