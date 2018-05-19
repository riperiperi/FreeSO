using FSO.Common.Utils;
using MIConvexHull;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoronoiLib.Structures;

namespace FSO.Client.Rendering.City.Graph
{
    public class VoronoiCellGraph
    {
        //  Graph modelled as list of edges
        int[,] graph =
            {
                {1, 2}, {1, 3}, {1, 4}, {2, 3},
                {3, 4}, {2, 6}, {4, 6}, {7, 8},
                {8, 9}, {9, 7}
            };

        List<int[]> cycles = new List<int[]>();

        public List<CompleteVCell> Result;

        public VoronoiCellGraph(List<Vector2> verts)
        {
            var mesh = VoronoiLib.FortunesAlgorithm.Run(verts.Select(x => new VoronoiLib.Structures.FortuneSite(x.X, x.Y)).ToList(), 0, 0, 512, 512);

            var ptDict = new Dictionary<Vector2, int>();
            var pts = new List<Vector2>();
            var tempGraph = new int[mesh.Count,2];

            Func<VPoint, int> getInDict = (VPoint v) => {
                var vec = new Vector2((float)v.X, (float)v.Y);
                int result;
                if (!ptDict.TryGetValue(vec, out result))
                {
                    result = pts.Count;
                    pts.Add(vec);
                    ptDict[vec] = result;
                }
                return result;
            };

            int gi = 0;
            foreach (var edge in mesh)
            {
                tempGraph[gi, 0] = getInDict(edge.Start);
                tempGraph[gi, 1] = getInDict(edge.End);
                gi++;
            }

            var adj = new List<int>[pts.Count];
            for (int i = 0; i < adj.Length; i++) adj[i] = new List<int>(); 
            for (int i=0; i<mesh.Count; i++)
            {
                adj[tempGraph[i, 0]].Add(tempGraph[i, 1]);
                adj[tempGraph[i, 1]].Add(tempGraph[i, 0]);
            }

            Func<int, int, double> getAngle = (i, i2) =>
            {
                var diff = pts[i2] - pts[i];
                return Math.Atan2(diff.Y, diff.X);
            };

            var visited = new HashSet<Tuple<int, int>>();
            for (int i=0; i<pts.Count; i++)
            {
                var a = adj[i];
                for (int j=0; j<a.Count; j++)
                {
                    var i2 = a[j];
                    var eT = new Tuple<int, int>(i, i2);
                    if (visited.Contains(eT)) continue;

                    var last = i;
                    var cur = i2;
                    visited.Add(eT);
                    var cycle = new List<int>() { i };
                    while (cur != i)
                    {
                        var ang = getAngle(last, cur);
                        var curAdj = adj[cur];
                        cycle.Add(cur);

                        var best = double.PositiveInfinity;
                        int bestInd = -1;
                        for (int k=0; k<curAdj.Count; k++)
                        {
                            var candidate = curAdj[k];
                            if (candidate == last) continue;
                            var ang2 = getAngle(cur, candidate);
                            var diff = DirectionUtils.Difference(ang2, ang);
                            if (diff >= 0 && diff < best)
                            {
                                best = diff;
                                bestInd = candidate;
                            }
                        }
                        if (bestInd == -1)
                        {
                            cycle = null;
                            break;
                        }
                        visited.Add(new Tuple<int, int>(cur, bestInd));
                        last = cur;
                        cur = bestInd;
                    }
                    if (cycle != null)
                    {
                        cycles.Add(cycle.ToArray());
                    }
                }
            }

            graph = tempGraph;
            
            //ok, so the graph is defined really weirdly.

            /*
            for (int i = 0; i < graph.GetLength(0); i++)
                for (int j = 0; j < graph.GetLength(1); j++)
                {
                    findNewCycles(new int[] { graph[i, j] });
                }
                */

            Result = new List<CompleteVCell>();
            foreach (int[] cy in cycles)
            {
                Result.Add(new CompleteVCell() { Cycle = cy.Select(x => pts[x]).ToArray() });
            }
        }

        void findNewCycles(int[] path)
        {
            int n = path[0];
            int x;
            int[] sub = new int[path.Length + 1];

            for (int i = 0; i < graph.GetLength(0); i++)
                for (int y = 0; y <= 1; y++)
                    if (graph[i, y] == n)
                    //  edge referes to our current node
                    {
                        x = graph[i, (y + 1) % 2];
                        if (!visited(x, path))
                        //  neighbor node not on path yet
                        {
                            sub[0] = x;
                            Array.Copy(path, 0, sub, 1, path.Length);
                            //  explore extended path
                            findNewCycles(sub);
                        }
                        else if ((path.Length > 2) && (x == path[path.Length - 1]))
                        //  cycle found
                        {
                            int[] p = normalize(path);
                            int[] inv = invert(p);
                            if (isNew(p) && isNew(inv))
                                cycles.Add(p);
                        }
                    }
        }

        static bool equals(int[] a, int[] b)
        {
            bool ret = (a[0] == b[0]) && (a.Length == b.Length);

            for (int i = 1; ret && (i < a.Length); i++)
                if (a[i] != b[i])
                {
                    ret = false;
                }

            return ret;
        }

        static int[] invert(int[] path)
        {
            int[] p = new int[path.Length];

            for (int i = 0; i < path.Length; i++)
                p[i] = path[path.Length - 1 - i];

            return normalize(p);
        }

        //  rotate cycle path such that it begins with the smallest node
        static int[] normalize(int[] path)
        {
            int[] p = new int[path.Length];
            int x = smallest(path);
            int n;

            Array.Copy(path, 0, p, 0, path.Length);

            while (p[0] != x)
            {
                n = p[0];
                Array.Copy(p, 1, p, 0, p.Length - 1);
                p[p.Length - 1] = n;
            }

            return p;
        }

        bool isNew(int[] path)
        {
            bool ret = true;

            foreach (int[] p in cycles)
                if (equals(p, path))
                {
                    ret = false;
                    break;
                }

            return ret;
        }

        static int smallest(int[] path)
        {
            int min = path[0];

            foreach (int p in path)
                if (p < min)
                    min = p;

            return min;
        }

        static bool visited(int n, int[] path)
        {
            bool ret = false;

            foreach (int p in path)
                if (p == n)
                {
                    ret = true;
                    break;
                }

            return ret;
        }
    }

    public class CompleteVCell
    {
        public Vector2[] Cycle;

        public int Ind; //index of the closest point (usually neighbourhood id)
        public VertexBuffer Vertices;
        public IndexBuffer Indices;

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
        }
    }
}
