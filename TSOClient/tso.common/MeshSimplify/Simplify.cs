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
        public List<MSTriangle> triangles = new List<MSTriangle>();
        public List<MSVertex> vertices = new List<MSVertex>();
        public List<MSRef> refs = new List<MSRef>();

        public void simplify_mesh(int target_count, double agressiveness = 7, int iterations = 100)
        {
            //for (int i=0; i<triangles.Count; i++) triangles[i].deleted = false;

            // main iteration loop 

            int deleted_triangles = 0;
            var deleted0 = new List<int>(); 
            var deleted1 = new List<int>();
            int triangle_count = triangles.Count;

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
                for (var i=0; i < triangles.Count; i++)
                    triangles[i].dirty = false;

                //
                // All triangles with edges below the threshold will be removed
                //
                // The following numbers works well for most models.
                // If it does not, try to adjust the 3 parameters
                //
                double threshold = 0.000000001 * Math.Pow((double)iteration + 3, agressiveness);

                // remove vertices & mark deleted triangles			
                for (var i = 0; i < triangles.Count; i++)

            {
                    var t = triangles[i];
                    if (t.err[3] > threshold) continue;
                    if (t.deleted) continue;
                    if (t.dirty) continue;

                    for (int j = 0; j < 3; j++)
                    {
                        if (t.err[j] < threshold)
                        {
                            int i0 = t.v[j]; var v0 = vertices[i0];
                            int i1 = t.v[(j + 1) % 3]; var v1 = vertices[i1];

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
                            if (flipped(p, i0, i1, v0, v1, deleted0)) continue;
                            if (flipped(p, i1, i0, v1, v0, deleted1)) continue;

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

                            update_triangles(i0, v0, deleted0, ref deleted_triangles);
                            update_triangles(i0, v1, deleted1, ref deleted_triangles);

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

        bool flipped(Vector3 p, int i0, int i1, MSVertex v0, MSVertex v1, List<int> deleted)
        {
            int bordercount = 0;
            for (int k=0; k<v0.tcount; k++)
            {
                var t = triangles[refs[v0.tstart + k].tid];
                if (t.deleted) continue;

                int s = refs[v0.tstart + k].tvertex;
                int id1 = t.v[(s + 1) % 3];
                int id2 = t.v[(s + 2) % 3];

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

        void update_triangles(int i0, MSVertex v, List<int> deleted, ref int deleted_triangles)
        {
            Vector3 p = Vector3.Zero;
            for (int k = 0; k < v.tcount; k++)
            {
                var r = refs[v.tstart + k];
                var t = triangles[r.tid];
                if (t.deleted) continue;
                if (k < deleted.Count && deleted[k] > 0)
                {
                    t.deleted = true;
                    deleted_triangles++;
                    continue;
                }
                t.v[r.tvertex] = i0;
                t.dirty = true;
                t.err[0] = calculate_error(t.v[0], t.v[1], ref p);
                t.err[1] = calculate_error(t.v[1], t.v[2], ref p);
                t.err[2] = calculate_error(t.v[2], t.v[0], ref p);
                t.err[3] = Math.Min(t.err[0], Math.Min(t.err[1], t.err[2]));
                refs.Add(r);
            }
        }

        // compact triangles, compute edge error and build reference list

        void update_mesh(int iteration)
        {
            if (iteration > 0) // compact triangles
            {
                int dst = 0;
                for (int i = 0; i<triangles.Count; i++) {
                    if (!triangles[i].deleted)
                    {
                        triangles[dst++] = triangles[i];
                    }
                }
                var c = triangles.Count;
                triangles.RemoveRange(dst, c - dst);
            }
            //
            // Init Quadrics by Plane & Edge Errors
            //
            // required at the beginning ( iteration == 0 )
            // recomputing during the simplification is not required,
            // but mostly improves the result for closed meshes
            //
            if (iteration == 0)
            {
                for (int i=0; i<vertices.Count; i++)
                    vertices[i].q = new SymmetricMatrix(0.0);

                for (int i=0; i<triangles.Count; i++)
                {
                    var t = triangles[i];
                    Vector3 n;
                    Vector3[] p = new Vector3[3];
                    for (int j=0; j<3; j++) p[j] = vertices[t.v[j]].p;
                    n = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
                    n.Normalize();
                    t.n = n;
                    for (int j = 0; j < 3; j++) vertices[t.v[j]].q =
                         vertices[t.v[j]].q + new SymmetricMatrix(n.X, n.Y, n.Z, -Vector3.Dot(n,p[0]));
                }

                for (int i = 0; i < triangles.Count; i++)
                {
                    // Calc Edge Error
                    var t = triangles[i]; Vector3 p = Vector3.Zero;
                    for (int j = 0; j < 3; j++) t.err[j] = calculate_error(t.v[j], t.v[(j + 1) % 3], ref p);
                    t.err[3] = Math.Min(t.err[0], Math.Min(t.err[1], t.err[2]));
                }
            }

            // Init Reference ID list	
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].tstart = 0;
                vertices[i].tcount = 0;
            }
            for (int i = 0; i < triangles.Count; i++)
            {
                var t = triangles[i];
                for (int j = 0; j < 3; j++) vertices[t.v[j]].tcount++;
            }
            int tstart = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                v.tstart = tstart;
                tstart += v.tcount;
                v.tcount = 0;
            }

            // Write References
            refs.Clear();
            for (int i = 0; i < triangles.Count * 3; i++)
                refs.Add(new MSRef());
            for (int i = 0; i < triangles.Count; i++)
            {
                var t = triangles[i];
                for (int j = 0; j < 3; j++)
                {
                    var v = vertices[t.v[j]];
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

                for (int i = 0; i < vertices.Count; i++)
                    vertices[i].border = false;

                for (int i = 0; i < vertices.Count; i++)
                {
                    var v = vertices[i];
                    vcount.Clear();
                    vids.Clear();
                    for (int j = 0; j < v.tcount; j++)
                    {
                        int k = refs[v.tstart + j].tid;
                        var t = triangles[k];
                        for (k = 0; k < 3; k++)
                        {
                            int ofs = 0, id = t.v[k];
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
            }
        }

        // Finally compact mesh before exiting

        void compact_mesh()
        {
            int dst = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].tcount = 0;
            }
            for (int i = 0; i < triangles.Count; i++)
            {
                if (!triangles[i].deleted)
                {
                    var t = triangles[i];
                    triangles[dst++] = t;
                    for (int j = 0; j < 3; j++) vertices[t.v[j]].tcount = 1;
                }
            }
            triangles.RemoveRange(dst, triangles.Count - dst);
            dst = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].tcount > 0)
                {
                    vertices[i].tstart = dst;
                    vertices[dst].p = vertices[i].p;
                    vertices[dst].t = vertices[i].t;
                    dst++;
                }
            }
            for (int i = 0; i < triangles.Count; i++)
            {
                var t = triangles[i];
                for (int j = 0; j < 3; j++) t.v[j] = vertices[t.v[j]].tstart;
            }
            vertices.RemoveRange(dst, vertices.Count - dst);
        }

        // Error between vertex and Quadric

        double vertex_error(SymmetricMatrix q, double x, double y, double z)
        {
            return q[0] * x * x + 2 * q[1] * x * y + 2 * q[2] * x * z + 2 * q[3] * x + q[4] * y * y
                 + 2 * q[5] * y * z + 2 * q[6] * y + q[7] * z * z + 2 * q[8] * z + q[9];
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
