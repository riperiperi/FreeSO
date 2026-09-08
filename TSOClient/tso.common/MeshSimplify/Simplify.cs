/*
 * ==== Fast Quadratic Mesh Simplification ====
 * Ported and extended from https://github.com/sp4cerat/Fast-Quadric-Mesh-Simplification/ 
 *
 * Typically used for simplifying meshes the 3D reconstruction generates.
 * 
 */

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FSO.Common.MeshSimplify
{
    public class Simplify
    {
        public MSTriangle[] triangles;
        private int triangleCount;

        public MSVertex[] vertices;
        private int vertexCount;

        public List<MSRef> refs = new List<MSRef>();

        public Simplify(MSTriangle[] triangles, MSVertex[] vertices, int triangleCount = 0)
        {
            this.triangles = triangles;
            this.triangleCount = triangleCount == 0 ? triangles.Length : triangleCount;
            this.vertices = vertices;
            this.vertexCount = vertices.Length;
        }

        public void simplify_mesh(int target_count, double agressiveness = 7, int iterations = 100)
        {
            //for (int i=0; i<triangles.Count; i++) triangles[i].deleted = false;

            // main iteration loop 

            int deleted_triangles = 0;
            var deleted0 = new List<int>(); 
            var deleted1 = new List<int>();
            int triangle_count = triangleCount;

            for (int iteration=0; iteration<iterations; iteration++)
            {
                // target number of triangles reached ? Then break
                if (triangle_count - deleted_triangles <= target_count) break;

                // update mesh once in a while
                if (iteration % 5 == 0)
                {
                    update_mesh(iteration);
                }

                // clear dirty flag
                for (var i=0; i < triangleCount; i++)
                    triangles[i].dirty = false;

                //
                // All triangles with edges below the threshold will be removed
                //
                // The following numbers works well for most models.
                // If it does not, try to adjust the 3 parameters
                //
                double threshold = 0.000000001 * Math.Pow((double)iteration + 3, agressiveness);

                // remove vertices & mark deleted triangles			
                for (var i = 0; i < triangleCount; i++)

            {
                    ref var t = ref triangles[i]; //readonly
                    if (t.err.e3 > threshold) continue;
                    if (t.deleted) continue;
                    if (t.dirty) continue;

                    for (int j = 0; j < 3; j++)
                    {
                        if (t.err.GetRef(j) < threshold)
                        {
                            int i0 = t.v.GetRef(j); ref var v0 = ref vertices[i0];
                            int i1 = t.v.GetRef((j + 1) % 3); ref var v1 = ref vertices[i1];

                            // Border check
                            if (v0.border != v1.border) continue;

                            // Compute vertex to collapse to
                            Vector3 p = Vector3.Zero;
                            calculate_error(i0, i1, ref p);

                            deleted0.Clear(); // normals temporarily
                            for (int n = 0; n < v0.tcount; n++) deleted0.Add(0);
                            deleted1.Clear(); // normals temporarily
                            for (int n = 0; n < v1.tcount; n++) deleted1.Add(0);

                            // dont remove if flipped
                            if (flipped(in p, i0, i1, in v0, in v1, deleted0)) continue;
                            if (flipped(in p, i1, i0, in v1, in v0, deleted1)) continue;

                            // not flipped, so remove edge
                            
                            var vec = v1.p - v0.p;
                            var vec2 = p - v0.p;
                            vec2 /= vec.Length();
                            vec /= vec.Length();
                            var lp = Vector3.Dot(vec, vec2);
                            v0.p = p;
                            v0.t = Vector2.Lerp(v0.t, v1.t, lp);
                            v0.q = v1.q + v0.q;
                            int tstart = refs.Count;

                            update_triangles(i0, in v0, deleted0, ref deleted_triangles);
                            update_triangles(i0, in v1, deleted1, ref deleted_triangles);

                            int tcount = refs.Count - tstart;

                            if (tcount <= v0.tcount)
                            {
                                // save ram
                                for (int tc=0; tc<tcount; tc++)
                                {
                                    refs[v0.tstart + tc] = refs[tstart + tc];
                                }
                            }
                            else
                                // append
                                v0.tstart = tstart;

                            v0.tcount = tcount;
                            break;
                        }
                    }
                    // done?
                    if (triangle_count - deleted_triangles <= target_count) break;
                }
            }

            // clean up mesh
            compact_mesh();
        }

        // Check if a triangle flips when this edge is removed

        bool flipped(in Vector3 p, int i0, int i1, in MSVertex v0, in MSVertex v1, List<int> deleted)
        {
            int bordercount = 0;
            for (int k=0; k<v0.tcount; k++)
            {
                ref var t = ref triangles[refs[v0.tstart + k].tid]; //readonly
                if (t.deleted) continue;

                int s = refs[v0.tstart + k].tvertex;
                int id1 = t.v.GetRef((s + 1) % 3);
                int id2 = t.v.GetRef((s + 2) % 3);

                if (id1 == i1 || id2 == i1) // delete ?
                {
                    bordercount++;
                    deleted[k]=1;
                    continue;
                }
                Vector3 d1 = vertices[id1].p - p; d1.Normalize();
                Vector3 d2 = vertices[id2].p - p; d2.Normalize();
                if (Math.Abs(Vector3.Dot(d1, d2)) > 0.999) return true;
                Vector3 n;
                n = Vector3.Cross(d1, d2);
                n.Normalize();
                deleted[k] = 0;
                if (Vector3.Dot(n, t.n) < 0.2) return true;
            }
            return false;
        }

        // Update triangle connections and edge error after a edge is collapsed

        void update_triangles(int i0, in MSVertex v, List<int> deleted, ref int deleted_triangles)
        {
            Vector3 p = Vector3.Zero;
            for (int k = 0; k < v.tcount; k++)
            {
                var r = refs[v.tstart + k];
                ref var t = ref triangles[r.tid];
                if (t.deleted) continue;
                if (k < deleted.Count && deleted[k] > 0)
                {
                    t.deleted = true;
                    deleted_triangles++;
                    continue;
                }
                t.v.GetRef(r.tvertex) = i0;
                t.dirty = true;
                t.err.e0 = calculate_error(t.v.i0, t.v.i1, ref p);
                t.err.e1 = calculate_error(t.v.i1, t.v.i2, ref p);
                t.err.e2 = calculate_error(t.v.i2, t.v.i0, ref p);
                t.err.e3 = Math.Min(t.err.e0, Math.Min(t.err.e1, t.err.e2));
                refs.Add(r);
            }
        }

        // compact triangles, compute edge error and build reference list

        void update_mesh(int iteration)
        {
            if (iteration > 0) // compact triangles
            {
                int dst = 0;
                for (int i = 0; i<triangleCount; i++) {
                    if (!triangles[i].deleted)
                    {
                        triangles[dst++] = triangles[i];
                    }
                }

                triangleCount = dst;
            }

            // Init Reference ID list	
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tstart = 0;
                vertices[i].tcount = 0;
            }
            for (int i = 0; i < triangleCount; i++)
            {
                ref var t = ref triangles[i]; //readonly
                for (int j = 0; j < 3; j++) vertices[t.v.GetRef(j)].tcount++;
            }
            int tstart = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                ref var v = ref vertices[i];
                v.tstart = tstart;
                tstart += v.tcount;
                v.tcount = 0;
            }

            // Write References
            refs.Clear();
            for (int i = 0; i < triangleCount * 3; i++)
                refs.Add(new MSRef());
            for (int i = 0; i < triangleCount; i++)
            {
                ref var t = ref triangles[i]; //readonly
                for (int j = 0; j < 3; j++)
                {
                    ref var v = ref vertices[t.v.GetRef(j)];
                    refs[v.tstart + v.tcount] = new MSRef()
                    {
                        tid = i,
                        tvertex = j
                    };
                    v.tcount++;
                }
            }

            // Identify boundary : vertices[].border=0,1 
            if (iteration == 0)
            {
                List<int> vcount = new List<int>();
                List<int> vids = new List<int>();

                for (int i = 0; i < vertexCount; i++)
                    vertices[i].border = false;

                for (int i = 0; i < vertexCount; i++)
                {
                    ref var v = ref vertices[i];
                    vcount.Clear();
                    vids.Clear();
                    for (int j = 0; j < v.tcount; j++)
                    {
                        int k = refs[v.tstart + j].tid;
                        ref var t = ref triangles[k]; //readonly
                        for (k = 0; k < 3; k++)
                        {
                            int ofs = 0, id = t.v.GetRef(k);
                            while (ofs < vcount.Count)
                            {
                                if (vids[ofs] == id) break;
                                ofs++;
                            }
                            if (ofs == vcount.Count)
                            {
                                vcount.Add(1);
                                vids.Add(id);
                            }
                            else
                                vcount[ofs]++;
                        }
                    }
                    for (int j = 0; j < vcount.Count; j++)
                    {
                        if (vcount[j] == 1)
                            vertices[vids[j]].border = true;
                    }
                }

                // Initialize errors
                for (int i = 0; i < vertexCount; i++)
                    vertices[i].q = new SymmetricMatrix();

                for (int i = 0; i < triangleCount; i++)
                {
                    ref var t = ref triangles[i];
                    Vector3 n;

                    Vector3 p0 = vertices[t.v.i0].p;
                    Vector3 p1 = vertices[t.v.i1].p;
                    Vector3 p2 = vertices[t.v.i2].p;

                    n = Vector3.Cross(p1 - p0, p2 - p0);
                    n.Normalize();
                    if (float.IsNaN(n.X))
                    {
                        n = Vector3.Up;
                    }
                    t.n = n;
                    for (int j = 0; j < 3; j++) vertices[t.v.GetRef(j)].q =
                         vertices[t.v.GetRef(j)].q + new SymmetricMatrix(n.X, n.Y, n.Z, -Vector3.Dot(n, p0));
                }

                for (int i = 0; i < triangleCount; i++)
                {
                    // Calc Edge Error
                    ref var t = ref triangles[i]; Vector3 p = Vector3.Zero;

                    for (int j = 0; j < 3; j++) t.err.GetRef(j) = calculate_error(t.v.GetRef(j), t.v.GetRef((j + 1) % 3), ref p);
                    t.err.e3 = Math.Min(t.err.e0, Math.Min(t.err.e1, t.err.e2));
                }
            }
        }

        // Finally compact mesh before exiting

        void compact_mesh()
        {
            int dst = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tcount = 0;
            }
            for (int i = 0; i < triangleCount; i++)
            {
                if (!triangles[i].deleted)
                {
                    var t = triangles[i];
                    triangles[dst++] = t;
                    for (int j = 0; j < 3; j++) vertices[t.v.GetRef(j)].tcount = 1;
                }
            }
            triangleCount = dst;
            Array.Resize(ref triangles, triangleCount);

            dst = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                if (vertices[i].tcount > 0)
                {
                    vertices[i].tstart = dst;
                    vertices[dst].p = vertices[i].p;
                    vertices[dst].t = vertices[i].t;
                    dst++;
                }
            }
            for (int i = 0; i < triangleCount; i++)
            {
                ref var t = ref triangles[i];
                for (int j = 0; j < 3; j++) t.v.GetRef(j) = vertices[t.v.GetRef(j)].tstart;
            }
            vertexCount = dst;
            Array.Resize(ref vertices, vertexCount);
        }

        // Error between vertex and Quadric

        double vertex_error(SymmetricMatrix q, double x, double y, double z)
        {
            return q[0] * x * x + 
                2 * q[1] * x * y +
                2 * q[2] * x * z +
                2 * q[3] * x +
                q[4] * y * y +
                2 * q[5] * y * z +
                2 * q[6] * y +
                q[7] * z * z +
                2 * q[8] * z +
                q[9];
        }

        // Error for one edge

        double calculate_error(int id_v1, int id_v2, ref Vector3 p_result)
        {
            // compute interpolated vertex 

            SymmetricMatrix q = vertices[id_v1].q + vertices[id_v2].q;
            bool border = vertices[id_v1].border && vertices[id_v2].border;
            double error = 0;
            double det = q.det(0, 1, 2, 1, 4, 5, 2, 5, 7);

            if (det != 0 && !border)
            {
                // q_delta is invertible
                p_result.X = (float)(-1 / det * (q.det(1, 2, 3, 4, 5, 6, 5, 7, 8))); // vx = A41/det(q_delta) 
                p_result.Y = (float)(1 / det * (q.det(0, 2, 3, 1, 5, 6, 2, 7, 8)));  // vy = A42/det(q_delta) 
                p_result.Z = (float)(-1 / det * (q.det(0, 1, 3, 1, 4, 6, 2, 5, 8))); // vz = A43/det(q_delta) 
                error = vertex_error(q, p_result.X, p_result.Y, p_result.Z);
            }
            else
            {
                // det = 0 -> try to find best result
                Vector3 p1 = vertices[id_v1].p;
                Vector3 p2 = vertices[id_v2].p;
                Vector3 p3 = (p1 + p2) / 2;
                double error1 = vertex_error(q, p1.X, p1.Y, p1.Z);
                double error2 = vertex_error(q, p2.X, p2.Y, p2.Z);
                double error3 = vertex_error(q, p3.X, p3.Y, p3.Z);
                error = Math.Min(error1, Math.Min(error2, error3));
                if (error1 == error) p_result = p1;
                if (error2 == error) p_result = p2;
                if (error3 == error) p_result = p3;
            }

            return error;
        }

    }
}
