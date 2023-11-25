using FSO.Common.WorldGeometry.Paths;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Common.WorldGeometry
{
    public class RoadMesh
    {
        public int LastIndex;
        public List<int> Indices = new List<int>();
        public List<MeshPoint> Vertices = new List<MeshPoint>();
    }

    public class RoadDetectedIntersection
    {
        public LinePath MainPath;
        public LinePath SubPath;
        public RoadGeometryTemplate Template;
        public float MainDist;
        public float SubDist;
        public bool ThreeWay;

        public Vector2 Center;
        public Vector2 AlignmentY; //how to transform Y coords
        public Vector2 AlignmentX; //how to transform X coords
    }

    public class RoadGeometry
    {
        public List<LinePath> Paths;
        public List<RoadDetectedIntersection> Intersections;
        public List<RoadGeometryTemplate> Templates;

        public RoadGeometry(List<LinePath> paths, List<RoadGeometryTemplate> templates)
        {
            Paths = paths;
            Templates = templates;
        }

        private float IntersectionDistance(Vector3 inter1, Vector3 inter2)
        {
            return Vector2.Distance(new Vector2(inter1.X, inter1.Y), new Vector2(inter2.X, inter2.Y));
        }

        public void GenerateIntersections()
        {
            Intersections = new List<RoadDetectedIntersection>();
            for (int i = 0; i < Paths.Count; i++)
            {
                var path1 = Paths[i];
                for (int j = i + 1; j < Paths.Count; j++)
                {
                    var path2 = Paths[j];

                    var inters = path1.Intersections(path2);
                    foreach (var inter in inters)
                    {
                        //find corresponding intersection in path2
                        var inter2n = path2.Intersections(path1).Cast<Vector3?>().FirstOrDefault(x => IntersectionDistance(x.Value, inter) < 1);
                        if (inter2n != null)
                        {
                            var inter2 = inter2n.Value;

                            int primaryLine = 0;
                            if (inter2.Z < 1 || inter2.Z > path2.Length-1)
                            {
                                primaryLine = 1;
                            }
                            if (inter.Z < 1 || inter.Z > path1.Length-1)
                            {
                                if (primaryLine != 0)
                                {
                                    throw new Exception("2 way intersection currently not supported. Make a curve instead.");
                                }
                                primaryLine = 2;
                            }

                            bool threeWay = primaryLine != 0;
                            if (!threeWay) primaryLine = 1;

                            var mainPath = (primaryLine == 1) ? path1 : path2;
                            var subPath = (primaryLine == 2) ? path1 : path2;
                            var mainDist = (primaryLine == 1) ? inter.Z : inter2.Z;
                            var subDist = (primaryLine == 2) ? inter.Z : inter2.Z;

                            var primaryAlign = mainPath.GetPositionNormalAt(mainDist);

                            var vert = primaryAlign.Item2;
                            vert = new Vector2(vert.Y, -vert.X);

                            float xflip = 1;
                            if (threeWay)
                            {
                                var normalSub = subPath.GetPositionNormalAt(subDist).Item2;
                                if (subDist < 1) normalSub = -normalSub;

                                if (Vector2.Dot(vert, normalSub) < 0) xflip = -1;
                            }

                            Intersections.Add(new RoadDetectedIntersection()
                            {
                                MainPath = mainPath,
                                SubPath = subPath,
                                MainDist = mainDist,
                                SubDist = subDist,
                                ThreeWay = threeWay,

                                Center = new Vector2(inter.X, inter.Y),
                                AlignmentY = vert, //how to transform Y coords
                                AlignmentX = primaryAlign.Item2 * xflip //how to transform X coords
                            });
                        }
                    }
                }
            }

            //split the road paths based on these intersections.
            foreach (var intersection in Intersections)
            {
                Paths.Remove(intersection.MainPath);
                Paths.Remove(intersection.SubPath);

                intersection.Template = Templates[Math.Max(intersection.MainPath.TemplateNum, intersection.SubPath.TemplateNum)];
                
                var mSplit = intersection.MainPath.Split(intersection.MainDist - intersection.MainPath.StartOffset, intersection.Template.IntersectionSize);
                var sSplit = intersection.SubPath.Split(intersection.SubDist - intersection.SubPath.StartOffset, intersection.Template.IntersectionFromSize);
                Paths.AddRange(mSplit);
                Paths.AddRange(sSplit);
                if (mSplit.Any(x => float.IsNaN(x.Length)) || sSplit.Any(x => float.IsNaN(x.Length))) { }

                //update intersections that use these paths to reference the new split paths
                foreach (var inter2 in Intersections)
                {
                    if (inter2 == intersection) continue;
                    if (inter2.MainPath == intersection.MainPath)
                    {
                        if (inter2.MainDist > intersection.MainDist) inter2.MainPath = mSplit.Last();
                        else inter2.MainPath = mSplit[0];
                    }
                    if (inter2.MainPath == intersection.SubPath)
                    {
                        if (inter2.MainDist > intersection.SubDist) inter2.MainPath = sSplit.Last();
                        else inter2.MainPath = sSplit[0];
                    }

                    if (inter2.SubPath == intersection.MainPath)
                    {
                        if (inter2.SubDist > intersection.MainDist) inter2.SubPath = mSplit.Last();
                        else inter2.SubPath = mSplit[0];
                    }
                    if (inter2.SubPath == intersection.SubPath)
                    {
                        if (inter2.SubDist > intersection.SubDist) inter2.SubPath = sSplit.Last();
                        else inter2.SubPath = sSplit[0];
                    }
                }
            }
        }

        public Dictionary<ushort, RoadMesh> Meshes;

        private void AddTriangle(List<int> indices, int i1, int i2, int i3)
        {
            indices.Add(i1);
            indices.Add(i2);
            indices.Add(i3);
        }

        public void GenerateRoadGeometry()
        {
            Meshes = new Dictionary<ushort, RoadMesh>();

            foreach (var seg in Templates[0].Segments)
            {
                foreach (var line in seg.Lines)
                {
                    if (!Meshes.ContainsKey(line.FloorTile)) Meshes[line.FloorTile] = new RoadMesh();
                }
            }

            foreach (var path in Paths)
            {
                path.PrepareJoins();
                var template = Templates[path.TemplateNum];

                if (path.Segments.Count == 0) continue;
                if (path.Length < 1) { }
                if (!path.SharpStart)
                {
                    var seg = path.Segments.First();
                    CapEnd(template, seg.Start, -seg.StartNormal);
                }

                //generate the line

                float linePosition = 0;
                float virtualPosition = 0;// path.StartOffset;
                var startSegment = template.GetSegmentForOffset(virtualPosition);
                RoadGeometryTemplateSegment currentSegment = startSegment.Item1;
                float remaining = startSegment.Item2;

                bool end;
                int i = 0;
                do
                {
                    end = linePosition + remaining >= path.Length;
                    if (end) remaining = path.Length - linePosition;

                    foreach (var mesh in Meshes.Values) mesh.LastIndex = mesh.Vertices.Count;
                    for (int j = 0; j < 2; j++)
                    {
                        var basePos = path.GetPositionNormalAt(linePosition);
                        foreach (var line in currentSegment.Lines)
                        {
                            var mesh = Meshes[line.FloorTile];
                            if (j > 0)
                            {
                                //create triangles
                                AddTriangle(mesh.Indices, mesh.LastIndex, mesh.Vertices.Count, mesh.LastIndex + 1);
                                AddTriangle(mesh.Indices, mesh.Vertices.Count, mesh.Vertices.Count + 1, mesh.LastIndex + 1);

                                mesh.LastIndex += 2;
                            }

                            var spos2d = basePos.Item1 + basePos.Item2 * line.Start.X;
                            var stc = FloorTC(new Vector2(line.Start.X, virtualPosition) + line.UVOff);
                            mesh.Vertices.Add(new MeshPoint(new Vector3(spos2d.X, line.Start.Y, spos2d.Y), stc));

                            var epos2d = basePos.Item1 + basePos.Item2 * line.End.X;
                            var etc = FloorTC(new Vector2(line.End.X, virtualPosition) + line.UVOff);
                            mesh.Vertices.Add(new MeshPoint(new Vector3(epos2d.X, line.End.Y, epos2d.Y), etc));
                        }

                        i++;
                        if (j == 0)
                        {
                            virtualPosition += remaining;
                            linePosition += remaining;
                        }
                    }
                    currentSegment = currentSegment.Next;
                } while (!end);


                if (!path.SharpEnd)
                {
                    var seg = path.Segments.Last();
                    CapEnd(template, seg.End, seg.EndNormal);
                }
            }

            if (Intersections != null)
            {
                foreach (var intersection in Intersections)
                {
                    PlaceIntersection(intersection);
                }
            }
        }

        private Vector2 FloorTC(Vector2 vec)
        {
            return new Vector2(-0.5f + vec.X - vec.Y, 0.5f + vec.X + vec.Y) * 0.5f;
        }

        public void PlaceIntersection(RoadDetectedIntersection intersection) {
            var template = intersection.Template;
            var iTemplate = intersection.ThreeWay ? template.Intersection3Way : template.Intersection4Way;
            var off = new Vector2(template.IntersectionFromSize, template.IntersectionSize)/2;
            var ctr = intersection.Center;

            var xm = intersection.AlignmentX;
            var ym = intersection.AlignmentY;
            foreach (var rect in iTemplate)
            {
                RoadMesh mesh;
                if (!Meshes.TryGetValue(rect.FloorTile, out mesh))
                {
                    mesh = new RoadMesh();
                    Meshes[rect.FloorTile] = mesh;
                }

                var ind = mesh.Vertices.Count;
                var pos = rect.Rect.Location.ToVector2() - off + rect.Offset;
                var tcOff = off + new Vector2(0.5f, 0f) - rect.Offset;
                var pos2 = xm * pos.X + ym * pos.Y + ctr;
                mesh.Vertices.Add(new MeshPoint(new Vector3(pos2.X, 0, pos2.Y), FloorTC(pos + tcOff)));

                pos += new Vector2(rect.Rect.Width, 0);
                pos2 = xm * pos.X + ym * pos.Y + ctr;
                mesh.Vertices.Add(new MeshPoint(new Vector3(pos2.X, 0, pos2.Y), FloorTC(pos + tcOff)));

                pos += new Vector2(0, rect.Rect.Height);
                pos2 = xm * pos.X + ym * pos.Y + ctr;
                mesh.Vertices.Add(new MeshPoint(new Vector3(pos2.X, 0, pos2.Y), FloorTC(pos + tcOff)));

                pos += new Vector2(-rect.Rect.Width, 0);
                pos2 = xm * pos.X + ym * pos.Y + ctr;
                mesh.Vertices.Add(new MeshPoint(new Vector3(pos2.X, 0, pos2.Y), FloorTC(pos + tcOff)));

                AddTriangle(mesh.Indices, ind, ind + 1, ind + 2);
                AddTriangle(mesh.Indices, ind, ind + 2, ind + 3);
            }
        }

        public void CapEnd(RoadGeometryTemplate template, Vector2 position, Vector2 normal)
        {
            foreach (var mesh in Meshes.Values) mesh.LastIndex = mesh.Vertices.Count;

            var lines = template.EndLines;
            for (int i=0; i<=template.EndRepeats; i++) {
                var angle = (i * Math.PI) / template.EndRepeats;
                var c = (float)Math.Cos(angle);
                var s = (float)Math.Sin(angle);
                Vector2 xToCoord = new Vector2(c * normal.X - s * normal.Y, s * normal.X + c * normal.Y);
                Vector2 xToTc = new Vector2(c, s);

                foreach (var line in lines)
                {
                    var mesh = Meshes[line.FloorTile];
                    if (line.TriangleCap)
                    {
                        if (i == 0)
                        { //create the point we rotate around
                            line.TempIndex = mesh.Vertices.Count;
                            var pos2d = position + xToCoord * line.End.X;
                            var tc = FloorTC(xToTc * line.End.X + line.UVOff);
                            mesh.Vertices.Add(new MeshPoint(new Vector3(pos2d.X, line.End.Y, pos2d.Y), tc));
                            mesh.LastIndex++;
                        }

                        if (i > 0)
                        {
                            //create triangles
                            AddTriangle(mesh.Indices, mesh.LastIndex++, mesh.Vertices.Count, line.TempIndex);
                        }

                        var spos2d = position + xToCoord * line.Start.X;
                        var stc = FloorTC(xToTc * line.Start.X + line.UVOff);
                        mesh.Vertices.Add(new MeshPoint(new Vector3(spos2d.X, line.Start.Y, spos2d.Y), stc));
                    }
                    else
                    {
                        if (i > 0)
                        {
                            //create triangles
                            AddTriangle(mesh.Indices, mesh.LastIndex, mesh.Vertices.Count, mesh.LastIndex + 1);
                            AddTriangle(mesh.Indices, mesh.Vertices.Count, mesh.Vertices.Count + 1, mesh.LastIndex + 1);

                            mesh.LastIndex += 2;
                        }

                        var spos2d = position + xToCoord * line.Start.X;
                        var stc = FloorTC(new Vector2(line.Start.X, i) + line.UVOff);
                        mesh.Vertices.Add(new MeshPoint(new Vector3(spos2d.X, line.Start.Y, spos2d.Y), stc));

                        var epos2d = position + xToCoord * line.End.X;
                        var etc = FloorTC(new Vector2(line.End.X, i) + line.UVOff);
                        mesh.Vertices.Add(new MeshPoint(new Vector3(epos2d.X, line.End.Y, epos2d.Y), etc));
                    }
                }
            }
        }
    }

    public class RoadGeometryTemplate
    {
        private RoadGeometryTemplateSegment[] _Segments;
        
        public RoadGeometryTemplateSegment[] Segments
        {
            get
            {
                return _Segments;
            }
            set
            {
                RepeatLength = 0;
                for (int i=0; i<value.Length; i++)
                {
                    var seg = value[i];
                    RepeatLength += seg.Extent;
                    seg.Next = value[(i + 1) % value.Length];
                }
                value[value.Length - 1].Next = value[0];
                _Segments = value;
            }
        }
        public float RepeatLength; //sum of all segment extents.

        public RoadGeometryTemplateLine[] EndLines; //(x, y) lines to rotate around z = 0. eg. line at left half of road, rotated clockwise through to make a circular sweep finishing at the right.
        public int EndRepeats; //number of subdivisions the end semicircle is drawn with. Should be about PI * radius if you want to keep pavements consistent.

        public float IntersectionSize; //intersections are expected to be square and rotatable
        public float IntersectionFromSize; 
        public RoadGeometryTemplateRect[] Intersection4Way;
        /// <summary>
        /// Same as Intersection4Way, but inserted when there are only three connecting lines.
        /// This template represents the y direction being the route for the straight 2 lines, and then x positive being the third (to the right).
        /// This is appropriately flipped if the intersection is on the left.
        /// </summary>
        public RoadGeometryTemplateRect[] Intersection3Way;

        public Tuple<RoadGeometryTemplateSegment, float> GetSegmentForOffset(float offset)
        {
            var moffset = offset % RepeatLength;
            var result = Segments.First();
            float soFar = 0;

            foreach (var seg in Segments)
            {
                if (soFar + seg.Extent > moffset)
                {
                    //this segment has not ended yet
                    return new Tuple<RoadGeometryTemplateSegment, float>(seg, (soFar + seg.Extent) - moffset);
                }
                //otherwise move onto the next
                soFar += seg.Extent;
            }
            return new Tuple<RoadGeometryTemplateSegment, float>(Segments.Last(), (soFar + Segments.Last().Extent) - moffset);
        }
    }

    public class RoadGeometryTemplateSegment
    {
        public float Extent; //the extent of this segment before moving onto the next segment
        public RoadGeometryTemplateLine[] Lines; //(x, y) lines to extend into z. x is a horizontal offset depending on the direction of the line

        public RoadGeometryTemplateSegment Next;
    }

    public class RoadGeometryTemplateLine
    {
        public Vector2 Start;
        public Vector2 End;
        public Vector2 UVOff;
        public ushort FloorTile;

        public bool TriangleCap;
        public int TempIndex;

        /// <summary>
        /// Liney
        /// </summary>
        /// <param name="start">The start of this line.</param>
        /// <param name="end"></param>
        /// <param name="floorTile">The floor tile to use for this line.</param>
        public RoadGeometryTemplateLine(Vector2 start, Vector2 end, ushort floorTile)
        {
            Start = start;
            End = end;
            FloorTile = floorTile;

            TriangleCap = End == Vector2.Zero;
        }

        public RoadGeometryTemplateLine(Vector2 start, Vector2 end, Vector2 uvOff, ushort floorTile) : this(start, end, floorTile)
        {
            UVOff = uvOff;
        }
    }

    public class RoadGeometryTemplateRect
    {
        public Rectangle Rect;
        public ushort FloorTile;
        public Vector2 Offset;

        public RoadGeometryTemplateRect(Rectangle rect, ushort floorTile)
        {
            Rect = rect;
            FloorTile = floorTile;
        }

        public RoadGeometryTemplateRect(Rectangle rect, ushort floorTile, Vector2 offset)
        {
            Rect = rect;
            FloorTile = floorTile;
            Offset = offset;
        }
    }
}
