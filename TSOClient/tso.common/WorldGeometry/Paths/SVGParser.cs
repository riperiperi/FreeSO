using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace FSO.Common.WorldGeometry.Paths
{
    public enum SVGPathSegmentType
    {
        MoveTo,
        LineTo,
        CurveTo,
        Close
    }

    public class SVGPathSegment
    {
        public SVGPathSegmentType Type;
        public Vector2 Position;
        public Vector2 ControlPoint1;
        public Vector2 ControlPoint2;
    }

    public class SVGPath
    {
        public string ID;
        public List<SVGPathSegment> Segments;

        public SVGPath(string id, List<SVGPathSegment> segs)
        {
            ID = id;
            Segments = segs;
        }
    }

    public class SVGParser
    {
        public List<SVGPath> Paths;

        public SVGParser(string svgText)
        {
            var xml = new XmlDocument();
            xml.XmlResolver = null;
            xml.LoadXml(svgText);

            Paths = new List<SVGPath>();

            var paths = xml.GetElementsByTagName("path");
            foreach (XmlNode path in paths)
            {
                var str = path.Attributes["d"].InnerText.Replace(',', ' ');
                int template = 0;
                var id = path.Attributes["id"]?.InnerText;
                var elems = str.Split(' ');
                var pos = new Vector2(0, 0);

                var newPath = new List<SVGPathSegment>();
                for (int i = 0; i < elems.Length; i += 0)
                {
                    var type = elems[i++];
                    if (type.Length == 0) continue;
                    var relative = char.IsLower(type[0]);
                    if (!relative) pos = new Vector2();
                    switch (type.ToLower())
                    {
                        case "m":
                        case "l":
                            //lineto
                            pos += new Vector2(float.Parse(elems[i++], CultureInfo.InvariantCulture), float.Parse(elems[i++], CultureInfo.InvariantCulture));
                            newPath.Add(new SVGPathSegment()
                            {
                                Position = pos,
                                Type = (type.ToLower() == "l") ? SVGPathSegmentType.LineTo : SVGPathSegmentType.MoveTo
                            });
                            break;
                        case "c":
                            var cp1 = new Vector2(float.Parse(elems[i++], CultureInfo.InvariantCulture), float.Parse(elems[i++], CultureInfo.InvariantCulture)) + pos;
                            var cp2 = new Vector2(float.Parse(elems[i++], CultureInfo.InvariantCulture), float.Parse(elems[i++], CultureInfo.InvariantCulture)) + pos;
                            pos += new Vector2(float.Parse(elems[i++], CultureInfo.InvariantCulture), float.Parse(elems[i++], CultureInfo.InvariantCulture));

                            newPath.Add(new SVGPathSegment()
                            {
                                Position = pos,
                                ControlPoint1 = cp1,
                                ControlPoint2 = cp2,
                                Type = SVGPathSegmentType.CurveTo
                            });
                            break;
                        case "z":
                            //close
                            newPath.Add(new SVGPathSegment()
                            {
                                Type = SVGPathSegmentType.Close
                            });
                            break;
                    }
                }
                Paths.Add(new SVGPath(id, newPath));
            }
        }

        public LinePath ToLinePath(SVGPath inpath)
        {
            var segs = inpath.Segments;
            var line = new List<Vector2>();

            var closed = false;
            var pos = new Vector2(0, 0);
            foreach (var seg in segs)
            {
                switch (seg.Type)
                {
                    case SVGPathSegmentType.MoveTo:
                    case SVGPathSegmentType.LineTo:
                        line.Add(seg.Position);
                        break;
                    case SVGPathSegmentType.CurveTo:
                        //subdivided curve. currently 20 subdivisions.
                        var subdiv = 20;
                        var lastPos = line.Last();
                        for (int i=1; i<subdiv; i++)
                        {
                            var t = i / (float)subdiv;
                            var s = 1 - t;
                            line.Add(new Vector2(
                                (s * s * s) * lastPos.X + 3 * (s * s * t) * seg.ControlPoint1.X + 3 * (s * t * t) * seg.ControlPoint2.X + (t * t * t) * seg.Position.X,
                                (s * s * s) * lastPos.Y + 3 * (s * s * t) * seg.ControlPoint1.Y + 3 * (s * t * t) * seg.ControlPoint2.Y + (t * t * t) * seg.Position.Y
                                ));
                        }
                        break;
                    case SVGPathSegmentType.Close:
                        //finish at the start.
                        if (line.First() != line.Last()) line.Add(line.First());
                        closed = true;
                        break;
                }
            }
            var path = new LinePath(line);

            if (inpath.ID != null && inpath.ID.StartsWith("template"))
            {
                path.TemplateNum = int.Parse(inpath.ID.Substring(8));
            }

            if (closed && path.Segments.Count > 0) {
                var first = path.Segments.First();
                var last = path.Segments.Last();
                first.StartNormal = last.EndNormal = (first.StartNormal + last.EndNormal) / 2;
                path.SharpEnd = true;
                path.SharpStart = true;
            }
            return path;
        }

        public List<LinePath> ToLinePaths()
        {
            return Paths.Select(x => ToLinePath(x)).ToList();
        }
    }
}
