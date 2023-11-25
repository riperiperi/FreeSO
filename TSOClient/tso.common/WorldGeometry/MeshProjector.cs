using FSO.SimAntics.Model.Routing;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Common.WorldGeometry
{
    /// <summary>
    /// Projects one mesh onto another mesh on a given axis, potentially with an offset from the surface.
    /// Example use case: Projecting a road onto a terrain mesh
    /// </summary>
    public class MeshProjector
    {
        public MeshProjector(IEnumerable<BaseMeshTriangle> baseMesh, IEnumerable<MeshTriangle> projMesh)
        {
            foreach (var tri in baseMesh) tri.GenBounds();
            foreach (var tri in projMesh) tri.GenBounds();

            BaseMesh = baseMesh;
            ProjectMesh = projMesh;

            BaseSet = BaseTriangleSet.RoughBalanced(baseMesh.ToList());
        }

        IEnumerable<BaseMeshTriangle> BaseMesh;
        BaseTriangleSet BaseSet;
        IEnumerable<MeshTriangle> ProjectMesh;

        public List<int> Indices;
        public List<MeshPoint> Vertices;

        public void Project()
        {
            Indices = new List<int>();
            Vertices = new List<MeshPoint>();
            //find list of potential intersect tris for a projtri
            //build clipping edges for projtri

            foreach (var projTri in ProjectMesh)
            {
                //find candidate baseTris
                var candidates = BaseSet.AllIntersect(projTri);
                foreach (var baseTri in candidates)
                {
                    //if (projTri.RoughIntersects(baseTri))
                    //{
                        ClipTriangles(baseTri, projTri, Vertices, Indices);
                    //}
                }
            }
        }

        private void ClipTriangles(BaseMeshTriangle baseTri, MeshTriangle projTri, List<MeshPoint> outverts, List<int> inds)
        {
            //Sutherland–Hodgman algorithm
            //clip a triangle against another by iteratively clipping each edge of the second one

            //we want to clip against base tri
            var outputList = new MeshPolygon(projTri);
            var basePlane = new Plane(baseTri.Vertices[0], baseTri.Vertices[1], baseTri.Vertices[2]);
            
            for (int i=0; i<3; i++)
            {
                if (outputList.Points.Count == 0) return;
                var inputList = outputList;
                var edge = new ClipEdge(baseTri.Vertices[i], baseTri.Vertices[(i + 1) % 3]);
                outputList = new MeshPolygon();
                var lastPoint = inputList.Points.Last();
                int j = inputList.Points.Count-1;
                foreach (var point in inputList.Points)
                {
                    if (!edge.ShouldClip(point.Position))
                    {
                        if (edge.ShouldClip(lastPoint.Position))
                        {
                            outputList.Points.Add(edge.IntersectLine(inputList, j));
                        }
                        //we still need to project the point onto the surface...
                        var ray = new Ray(point.Position, new Vector3(0, -1, 0));
                        var intersect2 = ray.Intersects(basePlane);
                        if (intersect2 == null) {
                            ray.Direction *= -1;
                            intersect2 = ray.Intersects(basePlane);
                            if (intersect2 == null) { }
                            intersect2 = -(intersect2 ?? 0f);
                        }
                        point.Position.Y -= intersect2.Value;
                        outputList.Points.Add(point);
                    } else
                    {
                        if (!edge.ShouldClip(lastPoint.Position))
                        {
                            outputList.Points.Add(edge.IntersectLine(inputList, j));
                        }
                    }
                    j = (j + 1) % inputList.Points.Count;
                    lastPoint = point;
                }
            }

            if (outputList.Points.Count < 3) return; //?

            outputList.Triangulate(outverts, inds);
        }
    }

    public class ClipEdge
    {
        Vector3 EdgeVec;
        Vector2 DotVec;
        Vector3 EdgePos;
        Vector2 EdgePos2;

        public ClipEdge(Vector3 from, Vector3 to)
        {
            //xz
            //we assume the triangle is winding clockwise, so points on the left should be clipped

            EdgeVec = to - from;
            EdgePos = from;
            EdgePos2 = new Vector2(from.X, from.Z);
            DotVec = new Vector2(-EdgeVec.Z, EdgeVec.X);
        }

        public bool ShouldClip(Vector3 pos)
        {
            return (Vector2.Dot(DotVec, new Vector2(pos.X, pos.Z) - EdgePos2) < 0);
        }

        public MeshPoint IntersectLine(MeshPolygon tri, int lineInd)
        {
            var points = tri.Points;
            var lineInd2 = (lineInd + 1) % points.Count;
            var pt1 = tri.Points[lineInd];
            var pt2 = tri.Points[lineInd2];

            Vector3 a = EdgeVec; //line 1
            Vector3 b = pt2.Position - pt1.Position; //line 2
            Vector3 c = EdgePos - pt1.Position; //vec between starts

            //percent of line 1 where we intersect with line 2
            float ip = 1 / (-b.X * a.Z + a.X * b.Z); //projection
            float t = (b.X * c.Z - b.Z * c.X) * ip;

            //percent of line 2 where we intersect line 1
            float ip2 = 1 / (-a.X * b.Z + b.X * a.Z);
            float s = (a.X * (-c.Z) - a.Z * (-c.X)) * ip2;

            //pos + vec * t = pos2 + vec2 * s
            //vec * t - vec2 * s = pos2 - pos1
            float[] newTC = new float[pt1.TexCoords.Length];
            float ms = 1 - s;
            for (int i=0; i<newTC.Length; i++)
            {
                newTC[i] = pt1.TexCoords[i] * ms + pt2.TexCoords[i] * s;
            }

            return new MeshPoint(
                //position from the clip triangle (use t)
                EdgePos + EdgeVec * t,
                //texcoords from the two points in the poly (use s)
                newTC
            );

        }
    }

    public class BaseMeshTriangle
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;

        public Vector3[] Vertices;

        public void GenBounds()
        {
            x1 = Vertices[0].X;
            y1 = Vertices[0].Z;
            x2 = x1;
            y2 = y1;
            for (int i=1; i<Vertices.Length; i++)
            {
                var v = Vertices[i];
                if (v.X < x1) x1 = v.X;
                if (v.Z < y1) y1 = v.Z;
                if (v.X > x2) x2 = v.X;
                if (v.Z > y2) y2 = v.Z;
            }
        }

        public bool RoughIntersects(BaseMeshTriangle other)
        {
            return !(x1 > other.x2 || x2 < other.x1 || y1 > other.y2 || y2 < other.y1);
        }
    }

    public class MeshTriangle : BaseMeshTriangle
    {
        public float[][] TexCoords;
    }

    public class MeshPoint
    {
        public Vector3 Position;
        public float[] TexCoords;

        public MeshPoint(Vector3 pos, float[] texCoords)
        {
            Position = pos;
            TexCoords = texCoords;
        }

        public MeshPoint(Vector3 pos, Vector2 texCoords)
        {
            Position = pos;
            TexCoords = new float[] { texCoords.X, texCoords.Y };
        }
    }

    public class MeshPolygon {
        public List<MeshPoint> Points;

        public MeshPolygon()
        {
            Points = new List<MeshPoint>();
        }

        public MeshPolygon(MeshTriangle tri)
        {
            Points = new List<MeshPoint>();
            for (int i=0; i<3; i++)
            {
                Points.Add(new MeshPoint(tri.Vertices[i], tri.TexCoords[i]));
            }
        }

        public void Triangulate(List<MeshPoint> outverts, List<int> inds)
        {
            //simple fan triangle fill
            var baseInd = outverts.Count;
            outverts.AddRange(Points);

            for (int i=2; i<Points.Count; i++)
            {
                inds.Add(baseInd);
                inds.Add(baseInd+i-1);
                inds.Add(baseInd+i);
            }
        }
    }
}
