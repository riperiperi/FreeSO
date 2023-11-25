using FSO.Common.WorldGeometry;
using FSO.Common.WorldGeometry.Paths;
using FSO.Files.RC;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FSO.Client.Utils.TestFunctions
{
    public class ProjectionTest
    {
        public void TestHeightmapCircle()
        {
            OBJ heightmap, circle;
            using (var file = File.OpenRead(@"C:\Users\Rhys\Documents\planetest.obj"))
            {
                heightmap = new OBJ(file);
            }
            using (var file = File.OpenRead(@"C:\Users\Rhys\Documents\circletest.obj"))
            {
                circle = new OBJ(file);
            }

            var baseMesh = new List<BaseMeshTriangle>();
            var projMesh = new List<MeshTriangle>();

            foreach (var group in heightmap.FacesByObjgroup)
            {
                var otri = new List<Vector3>();
                foreach (var tri in group.Value)
                {
                    otri.Add(heightmap.Vertices[tri[0] - 1]);
                    if (otri.Count == 3)
                    {
                        baseMesh.Add(new BaseMeshTriangle() { Vertices = otri.ToArray() });
                        otri = new List<Vector3>();
                    }
                }
            }

            foreach (var group in circle.FacesByObjgroup)
            {
                var otri = new List<Vector3>();
                var otc = new List<float[]>();
                foreach (var tri in group.Value)
                {
                    otri.Add(circle.Vertices[tri[0] - 1]);
                    otc.Add(new float[] { circle.TextureCoords[tri[1] - 1].X, circle.TextureCoords[tri[1] - 1].Y });
                    if (otri.Count == 3)
                    {
                        projMesh.Add(new MeshTriangle()
                        {
                            Vertices = otri.ToArray(),
                            TexCoords = otc.ToArray()
                        });
                        otri = new List<Vector3>();
                        otc = new List<float[]>();
                    }
                }
            }

            var proj = new MeshProjector(baseMesh, projMesh);
            proj.Project();

            //write result
            using (var file = File.Open(@"C:\Users\Rhys\Documents\test.obj", FileMode.Create))
            {
                SaveResult(proj, file);
            }
        }

        public void TestRoads()
        {
            var path1 = new LinePath(new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(40, 0),
                new Vector2(44, 1),
                new Vector2(46, 2),
                new Vector2(46+10, 2+10)
            });

            var path2 = new LinePath(new List<Vector2>
            {
                new Vector2(20, -30),
                new Vector2(20, 29)
            });

            var geom = new RoadGeometry(new List<LinePath>() { path1, path2 }, TS1RoadTemplates.OLD_TOWN_DEFAULT_TEMPLATES);

            geom.GenerateIntersections();
            geom.GenerateRoadGeometry();

            //write result
            using (var file = File.Open(@"C:\Users\Rhys\Documents\testroad.obj", FileMode.Create))
            {
                SaveRoadResult(geom, file);
            }
        }

        public void TestCombo()
        {
            var map = BasicMap();

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var heightmap = new SimplifiedHeightmap(512, map);
            heightmap.BuildSecondDerivative();
            heightmap.GenerateFullTree();
            heightmap.GenerateMesh();

            timer.Stop();
            Console.WriteLine("!!! Heightmap took " + timer.ElapsedMilliseconds + "ms.");
            timer.Restart();

            var svg = new SVGParser(File.ReadAllText(@"C:\Users\Rhys\Documents\roads.svg"));
            var paths = svg.ToLinePaths();

            var geom = new RoadGeometry(paths, TS1RoadTemplates.OLD_TOWN_DEFAULT_TEMPLATES);

            geom.GenerateIntersections();
            geom.GenerateRoadGeometry();

            timer.Stop();
            Console.WriteLine("!!! Road took " + timer.ElapsedMilliseconds + "ms.");
            timer.Restart();

            List<MeshProjector> projectors = new List<MeshProjector>();

            foreach (var pair in geom.Meshes)
            {
                var mesh = pair.Value;

                var baseTris = new List<BaseMeshTriangle>();
                for (int i = 0; i < heightmap.Indices.Count; i += 3)
                {
                    baseTris.Add(new BaseMeshTriangle()
                    {
                        Vertices = new Vector3[] {
                        heightmap.Vertices[heightmap.Indices[i]],
                        heightmap.Vertices[heightmap.Indices[i+1]],
                        heightmap.Vertices[heightmap.Indices[i+2]],
                    }
                    });
                }

                var projTris = new List<MeshTriangle>();
                for (int i = 0; i < mesh.Indices.Count; i += 3)
                {
                    projTris.Add(new MeshTriangle()
                    {
                        Vertices = new Vector3[] {
                        mesh.Vertices[mesh.Indices[i]].Position,
                        mesh.Vertices[mesh.Indices[i+1]].Position,
                        mesh.Vertices[mesh.Indices[i+2]].Position,
                    },
                        TexCoords = new float[][]
                        {
                        mesh.Vertices[mesh.Indices[i]].TexCoords,
                        mesh.Vertices[mesh.Indices[i+1]].TexCoords,
                        mesh.Vertices[mesh.Indices[i+2]].TexCoords,
                        }
                    });
                }

                var proj = new MeshProjector(baseTris, projTris);
                proj.Project();
                projectors.Add(proj);
            }

            timer.Stop();
            Console.WriteLine("!!! Projection took " + timer.ElapsedMilliseconds + "ms.");

            //write result
            using (var file = File.Open(@"C:\Users\Rhys\Documents\combined.obj", FileMode.Create))
            {
                SaveResults(projectors, file);
            }
        }

        public void TestRoads2()
        {
            var svg = new SVGParser(File.ReadAllText(@"C:\Users\Rhys\Documents\roads.svg"));
            var paths = svg.ToLinePaths();

            var geom = new RoadGeometry(paths, TS1RoadTemplates.OLD_TOWN_DEFAULT_TEMPLATES);

            geom.GenerateIntersections();
            geom.GenerateRoadGeometry();

            //write result
            using (var file = File.Open(@"C:\Users\Rhys\Documents\testroad2.obj", FileMode.Create))
            {
                SaveRoadResult(geom, file);
            }
        }

        private ushort[] BasicMap()
        {
            var map = new ushort[512 * 512];
            var i = 0;
            for (var y = 0; y < 512; y++)
            {
                for (var x = 0; x < 512; x++)
                {
                    var xd = x - 256;
                    var yd = y - 256;
                    var dist = Math.Sqrt(xd * xd + yd * yd);
                    if (dist < 128)
                    {
                        map[i++] = 20000;
                    }
                    else if (dist < 192)
                    {
                        var cos = Math.Cos(((192 - dist) / 64.0) * Math.PI);
                        map[i++] = (ushort)((1 - cos) * 10000);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            return map;
        }

        public void TestHeightmapGen()
        {
            var map = BasicMap();

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var heightmap = new SimplifiedHeightmap(512, map);
            heightmap.BuildSecondDerivative();
            heightmap.GenerateFullTree();
            heightmap.GenerateMesh();
            timer.Stop();

            Console.WriteLine("=== 512x512 heightmap took " + timer.ElapsedMilliseconds);
            //write result
            using (var file = File.Open(@"C:\Users\Rhys\Documents\testhm2.obj", FileMode.Create))
            {
                SaveHMResult(heightmap, file);
            }
        }

        public void SaveRoadResult(RoadGeometry road, Stream stream)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Experimental mesh projection output.");

                io.WriteLine("s 1");

                var baseInd = 1;
                foreach (var mesh in road.Meshes)
                {
                    var verts = mesh.Value.Vertices;
                    var inds = mesh.Value.Indices;
                    io.WriteLine("o tile"+mesh.Key);
                    foreach (var vert in verts)
                    {
                        io.WriteLine("v " + vert.Position.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Z.ToString(CultureInfo.InvariantCulture));
                        io.WriteLine("vt " + vert.TexCoords[0].ToString(CultureInfo.InvariantCulture) + " " + vert.TexCoords[1].ToString(CultureInfo.InvariantCulture));
                    }

                    io.Write("f ");

                    var ticker = 0;
                    var j = 0;
                    foreach (var ind in inds)
                    {
                        var i = ind + baseInd;
                        io.Write(i + "/" + i + " ");
                        if (++ticker == 3)
                        {
                            io.WriteLine("");
                            if (j < inds.Count - 1) io.Write("f ");
                            ticker = 0;
                        }
                        j++;
                    }
                    baseInd += verts.Count;
                }
            }
        }

        public void SaveHMResult(SimplifiedHeightmap hm, Stream stream)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Experimental mesh projection output.");

                io.WriteLine("s 1");

                io.WriteLine("o projected");
                foreach (var vert in hm.Vertices)
                {
                    io.WriteLine("v " + vert.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Z.ToString(CultureInfo.InvariantCulture));
                }

                io.Write("f ");
                var baseInd = 1;
                var ticker = 0;
                var j = 0;
                foreach (var ind in hm.Indices)
                {
                    var i = ind + baseInd;
                    io.Write(i + " ");
                    if (++ticker == 3)
                    {
                        io.WriteLine("");
                        if (j < hm.Indices.Count - 1) io.Write("f ");
                        ticker = 0;
                    }
                    j++;
                }
            }
        }

        public void SaveResult(MeshProjector proj, Stream stream)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Experimental mesh projection output.");

                io.WriteLine("s 1");

                io.WriteLine("o projected");
                foreach (var vert in proj.Vertices)
                {
                    io.WriteLine("v " + vert.Position.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Z.ToString(CultureInfo.InvariantCulture));
                }
                foreach (var vert in proj.Vertices)
                {
                    io.WriteLine("vt " + vert.TexCoords[0].ToString(CultureInfo.InvariantCulture) + " " + (1 - vert.TexCoords[1]).ToString(CultureInfo.InvariantCulture));
                }

                io.Write("f ");
                var baseInd = 1;
                var ticker = 0;
                var j = 0;
                foreach (var ind in proj.Indices)
                {
                    var i = ind + baseInd;
                    io.Write(i + "/" + i + " ");
                    if (++ticker == 3)
                    {
                        io.WriteLine("");
                        if (j < proj.Indices.Count - 1) io.Write("f ");
                        ticker = 0;
                    }
                    j++;
                }
            }
        }

        public void SaveResults(List<MeshProjector> projs, Stream stream)
        {
            using (var io = new StreamWriter(stream))
            {
                io.WriteLine("# Experimental mesh projection output.");

                io.WriteLine("s 1");

                int oind = 0;
                var baseInd = 1;
                foreach (var proj in projs)
                {
                    io.WriteLine("o projected"+(oind++));
                    foreach (var vert in proj.Vertices)
                    {
                        io.WriteLine("v " + vert.Position.X.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Y.ToString(CultureInfo.InvariantCulture) + " " + vert.Position.Z.ToString(CultureInfo.InvariantCulture));
                    }
                    foreach (var vert in proj.Vertices)
                    {
                        io.WriteLine("vt " + vert.TexCoords[0].ToString(CultureInfo.InvariantCulture) + " " + (1 - vert.TexCoords[1]).ToString(CultureInfo.InvariantCulture));
                    }

                    io.Write("f ");
                    var ticker = 0;
                    var j = 0;
                    foreach (var ind in proj.Indices)
                    {
                        var i = ind + baseInd;
                        io.Write(i + "/" + i + " ");
                        if (++ticker == 3)
                        {
                            io.WriteLine("");
                            if (j < proj.Indices.Count - 1) io.Write("f ");
                            ticker = 0;
                        }
                        j++;
                    }
                    baseInd += proj.Vertices.Count;
                }
            }
        }
    }
}
