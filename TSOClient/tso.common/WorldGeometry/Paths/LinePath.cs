using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Common.WorldGeometry.Paths
{
    public class LinePath
    {
        public List<LinePathSegment> Segments = new List<LinePathSegment>();
        public bool SharpStart;
        public bool SharpEnd;
        public int TemplateNum;

        public float StartOffset;
        public float Length;

        public LinePath()
        {

        }

        public LinePath(List<Vector2> line)
        {
            for (int i=0; i<line.Count-1; i++)
            {
                var seg = new LinePathSegment(line[i], line[i + 1]);
                Length += seg.Length;
                Segments.Add(seg);
            }
        }

        public Tuple<Vector2, Vector2> GetPositionNormalAt(float offset)
        {
            foreach (var seg in Segments)
            {
                //is the given offset in this segment? 
                if (offset < seg.Length)
                {
                    var i = offset / seg.Length;
                    return new Tuple<Vector2, Vector2>(Vector2.Lerp(seg.Start, seg.End, i), Vector2.Lerp(seg.StartNormal, seg.EndNormal, i));
                }
                offset -= seg.Length;
            }
            var last = Segments.Last();
            return new Tuple<Vector2, Vector2>(last.End, last.EndNormal);
        }

        public List<LinePath> Split(float dist, float gap)
        {
            var result = new List<LinePath>();
            var startGap = dist - gap / 2;
            var endGap = dist + gap / 2;

            bool before = 0 < startGap;
            LinePath current = new LinePath();
            if (before)
            {
                current.SharpStart = SharpStart;
                current.SharpEnd = true;
                current.StartOffset = StartOffset;
            }
            else
            {
                current.SharpStart = true;
                current.SharpEnd = SharpEnd;
                current.StartOffset = StartOffset + endGap;
            }
            current.TemplateNum = TemplateNum;
           
            float soFar = 0;
            foreach (var segment in Segments)
            {
                if (before)
                {
                    if (soFar + segment.Length <= startGap)
                    {
                        //add this segment
                        current.Segments.Add(segment);
                    }
                    else
                    {
                        //this segment extends over the gap.
                        //an additional segment must be added to reach the start gap
                        if (soFar != startGap && segment.Length != 0)
                        {
                            var bridge = new LinePathSegment(segment.Start, Vector2.Lerp(segment.Start, segment.End, (startGap - soFar) / segment.Length));
                            bridge.StartNormal = segment.StartNormal;
                            current.Segments.Add(bridge);
                        }

                        current.Length = current.Segments.Sum(x => x.Length);
                        result.Add(current);
                        current = new LinePath();
                        current.SharpStart = true;
                        current.SharpEnd = SharpEnd;
                        current.StartOffset = StartOffset + endGap;
                        current.TemplateNum = TemplateNum;
                        before = false;
                    }
                }
                if (!before)
                {
                    if (current.Segments.Count == 0)
                    {
                        //waiting to get to a segment that ends after the gap.
                        if (soFar + segment.Length > endGap)
                        {
                            var bridge = new LinePathSegment(Vector2.Lerp(segment.Start, segment.End, (endGap - soFar) / segment.Length), segment.End);
                            bridge.EndNormal = segment.EndNormal;
                            current.Segments.Add(bridge);
                        }
                    }
                    else
                    {
                        //add this segment
                        current.Segments.Add(segment);
                    }
                }

                soFar += segment.Length;
            }
            current.Length = current.Segments.Sum(x => x.Length);
            result.Add(current);
            return result;
        }

        public List<Vector3> Intersections(LinePath other)
        {
            var epsilon = (0.9f * 0.9f) / 0.5f;

            //finds intersections between this linepath and another.
            var result = new List<Vector3>();
            float soFar = 0;
            for (int i=0; i<Segments.Count; i++)
            {
                var seg1 = Segments[i];
                for (int j=0; j<other.Segments.Count; j++)
                {
                    var seg2 = other.Segments[j];
                    var inter = seg1.Intersect(seg2);

                    if (inter != null)
                    {
                        var interc = inter.Value;
                        interc.Z += soFar;
                        result.Add(interc);
                    }
                }
                soFar += seg1.Length;
            }

            //remove dupes
            result = result.OrderBy(x => x.Z).ToList();
            for (int i = 0; i < result.Count - 1; i++)
            {
                var first = result[i];
                while (i < result.Count - 1)
                {
                    var second = result[i + 1];
                    var distance = second - first;
                    distance.Z = 0;
                    if (distance.LengthSquared() < epsilon)
                    {
                        result.RemoveAt(i);
                    }
                    else;
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public void PrepareJoins()
        {
            LinePathSegment last = null;
            foreach (var line in Segments)
            {
                if (last != null)
                {
                    last.EndNormal = line.StartNormal = Vector2.Normalize(last.EndNormal + line.StartNormal);
                }
                last = line;
            }
        }
    }

    public class LinePathSegment
    {
        public Vector2 Start;
        public Vector2 End;

        public Vector2 Direction;

        //normals are used when constucting geometry from a line. they face to the right from the line.
        //to create a seamless line, we average the end normal of this line and the start normal of the last, setting both to the result.
        public Vector2 StartNormal;
        public Vector2 EndNormal;

        public float Length;

        public LinePathSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            Direction = end - start;
            Length = Direction.Length();

            var dirn = Direction;
            dirn.Normalize();
            StartNormal = EndNormal = new Vector2(-dirn.Y, dirn.X);
        }

        public Vector3? Intersect(LinePathSegment other) //xy: point, z: distance along line
        {
            if (this.Length == 0 || other.Length == 0) return null;
            var epsilon = 0.0001f;

            Vector2 a = Direction;
            Vector2 b = other.Direction;
            Vector2 c = Start - other.Start;

            //percent of line 1 where we intersect with line 2
            float ip = 1 / (-b.X * a.Y + a.X * b.Y); //projection
            float t = (b.X * c.Y - b.Y * c.X) * ip;

            //percent of line 2 where we intersect line 1
            float ip2 = 1 / (-a.X * b.Y + b.X * a.Y);
            float s = (a.X * (-c.Y) - a.Y * (-c.X)) * ip2;

            if (float.IsNaN(t) || t < -epsilon || t > 1 + epsilon || float.IsNaN(s) || s < -epsilon || s > 1 + epsilon)
            {
                return null;
            }
            return new Vector3(Direction * t + Start, t * Length);
        }
    }
}
