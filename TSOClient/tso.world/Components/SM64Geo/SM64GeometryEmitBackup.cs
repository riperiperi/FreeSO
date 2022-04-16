using Mario.Geo;
using Mario.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.LotView.Components.SM64Geo
{
    // Two ideas for geometry modes.
    //
    // Immediate:
    // Slower, easier to impl initially.
    // Vertex/index data is updated each frame. Vertex data is multiplied by the matrix at the top of the stack on CPU.
    //
    // Deferred:
    // Faster, but more complicated. Can swap out geo later.
    // Vertex/index data is only generated on the first time, each vertex includes a index reference to a matrix copy to be multiplied each frame.
    // The geo structure is traversed to generate new matrices for each index.
    // Matrix indices are only allocated when a matrix has changed and a display list has been entered.
    internal class SM64GeometryEmitBackup : AbstractGeometryEmit, IDisposable
    {
        private Matrix[] MatStack = new Matrix[32];
        private int MatStackPos;
        private Light Ambient;
        private Light Directional;

        private int VertOffset = 0;
        private List<VertexPositionColor> AllVertices = new List<VertexPositionColor>();
        private List<int> Indices = new List<int>();

        // Deferred mode only
        private Matrix ActiveMatrix;
        private List<Matrix> Matrices = new List<Matrix>();
        private List<List<VertexPositionColor>> VerticesPerMatrix = new List<List<VertexPositionColor>>();
        private bool FirstList;
        private bool Dirty;

        private VertexBuffer GDVertices;
        private IndexBuffer GDIndices;
        private int PrimitiveCount;

        private int BaseIndex;

        public bool DeferredMode = true;

        public bool Reset()
        {
            Indices.Clear();
            VertOffset = 0;

            MatStackPos = 0;
            MatStack[0] = Matrix.Identity;

            if (DeferredMode)
            {
                Matrices.Clear();

                bool drawing = GDVertices == null;
                if (drawing)
                {
                    VerticesPerMatrix.Clear();
                }

                FirstList = true;
                Dirty = true;

                return drawing;
            }
            else
            {
                AllVertices.Clear();
                Dispose();
            }

            return true;
        }

        public override void EnterDisplayList(bool drawing)
        {
            if (DeferredMode)
            {
                if (!FirstList)
                {
                    if (drawing)
                    {
                        VerticesPerMatrix.Add(AllVertices);
                        VertOffset += AllVertices.Count;
                        AllVertices = new List<VertexPositionColor>();
                    }

                    Matrices.Add(ActiveMatrix);
                }
                FirstList = false;
                
                ActiveMatrix = MatStack[MatStackPos];
            }
        }

        public void End(bool drawing)
        {
            if (DeferredMode)
            {
                if (drawing)
                {
                    VerticesPerMatrix.Add(AllVertices);
                }

                Matrices.Add(ActiveMatrix);
            }
        }

        public override void Light(Light l, int n)
        {
            if (n == 1)
            {
                Directional = l;
            }
            else
            {
                Ambient = l;
            }
        }

        public override void MatrixRestore()
        {
            MatStackPos--;
        }

        public override void MatrixSave()
        {
            MatStack[MatStackPos + 1] = MatStack[MatStackPos];
            MatStackPos++;
        }

        public override void RotateTranslate(Vec3f translation, Vec3s rotation)
        {
            ref Matrix mat = ref MatStack[MatStackPos];

            /*
            mat *= Matrix.CreateFromYawPitchRoll(
                (rotation.X / 32768f) * (float)System.Math.PI,
                (rotation.Y / 32768f) * (float)System.Math.PI,
                (rotation.Z / 32768f) * (float)System.Math.PI);
            */

            var newMat = new Matrix();

            float sx = (float)System.Math.Sin((rotation.X / 32768f) * (float)System.Math.PI);
            float cx = (float)System.Math.Cos((rotation.X / 32768f) * (float)System.Math.PI);

            float sy = (float)System.Math.Sin((rotation.Y / 32768f) * (float)System.Math.PI);
            float cy = (float)System.Math.Cos((rotation.Y / 32768f) * (float)System.Math.PI);

            float sz = (float)System.Math.Sin((rotation.Z / 32768f) * (float)System.Math.PI);
            float cz = (float)System.Math.Cos((rotation.Z / 32768f) * (float)System.Math.PI);

            newMat.M11 = cy * cz;
            newMat.M12 = cy * sz;
            newMat.M13 = -sy;
            newMat.M14 = 0;

            newMat.M21 = sx * sy * cz - cx * sz;
            newMat.M22 = sx * sy * sz + cx * cz;
            newMat.M23 = sx * cy;
            newMat.M24 = 0;

            newMat.M31 = cx * sy * cz + sx * sz;
            newMat.M32 = cx * sy * sz - sx * cz;
            newMat.M33 = cx * cy;
            newMat.M34 = 0;

            newMat.M41 = translation.X;
            newMat.M42 = translation.Y;
            newMat.M43 = translation.Z;
            newMat.M44 = 1;

            mat = newMat * mat;

            /*
            mat *= Matrix.CreateRotationX((rotation.X / 32768f) * (float)System.Math.PI);
            mat *= Matrix.CreateRotationY((rotation.Y / 32768f) * (float)System.Math.PI);
            mat *= Matrix.CreateRotationZ((rotation.Z / 32768f) * (float)System.Math.PI);

            mat *= Matrix.CreateTranslation(translation.X, translation.Y, translation.Z);
            */
        }

        public override void Triangle(int v00, int v01, int v02)
        {
            Indices.Add(v00 + BaseIndex);
            Indices.Add(v01 + BaseIndex);
            Indices.Add(v02 + BaseIndex);
        }

        public override void Triangles(int v00, int v01, int v02, int v10, int v11, int v12)
        {
            Indices.Add(v00 + BaseIndex);
            Indices.Add(v01 + BaseIndex);
            Indices.Add(v02 + BaseIndex);

            Indices.Add(v10 + BaseIndex);
            Indices.Add(v11 + BaseIndex);
            Indices.Add(v12 + BaseIndex);
        }

        private Vector3 ToVector3(Vec3f pos)
        {
            return new Vector3(pos.X, pos.Y, pos.Z);
        }

        private Color ToColor(Cn colorNormal, ref Matrix mat)
        {
            // From the active lighting factor, and the normal applied to the model
            var normal = new Vector3((sbyte)colorNormal.R, (sbyte)colorNormal.G, (sbyte)colorNormal.B) / 0x7F;
            normal = Vector3.TransformNormal(normal, mat);

            var col = new Color(Directional.R, Directional.G, Directional.B, (byte)0xFF);

            col *= (normal.Y + 3) / 4;
            col.A = 0xFF;

            return col;
        }

        public override void Vertices(Vtx[] vertices, int n, int v0)
        {
            ref Matrix mat = ref MatStack[MatStackPos];

            BaseIndex = VertOffset + AllVertices.Count;

            int end = v0 + n;

            if (DeferredMode)
            {
                for (int i = v0; i < end; i++)
                {
                    ref Vtx vtx = ref vertices[i];
                    AllVertices.Add(new VertexPositionColor(ToVector3(vtx.Pos), ToColor(vtx.Cn, ref mat)));
                }
            }
            else
            {
                for (int i = v0; i < end; i++)
                {
                    ref Vtx vtx = ref vertices[i];
                    AllVertices.Add(new VertexPositionColor(Vector3.Transform(ToVector3(vtx.Pos), mat), ToColor(vtx.Cn, ref mat)));
                }
            }
        }

        private int GetVertCount()
        {
            return DeferredMode ? VerticesPerMatrix.Sum(x => x.Count) : AllVertices.Count;
        }

        private VertexPositionColor[] BatchProcess()
        {
            if (!DeferredMode) return AllVertices.ToArray();

            int totalVerts = VerticesPerMatrix.Sum(x => x.Count);

            var verts = new VertexPositionColor[totalVerts];

            int offset = 0;
            int matId = 0;
            foreach (var v in VerticesPerMatrix)
            {
                v.CopyTo(verts, offset);
                int end = offset + v.Count;

                Matrix mat = Matrices[matId++];

                for (int i = offset; i < end; i++)
                {
                    ref var pos = ref verts[i].Position;
                    pos = Vector3.Transform(pos, mat);
                }

                offset += v.Count;
            }

            return verts;
        }

        public List<Matrix> ExportMatrices()
        {
            return Matrices;
        }

        public void ImportMatrices(List<Matrix> matrices)
        {
            Matrices = matrices;
        }

        public Tuple<VertexBuffer, IndexBuffer, int> Complete(GraphicsDevice gd)
        {
            if (GDVertices == null)
            {
                int vertCount = GetVertCount();

                if (vertCount == 0)
                {
                    return new Tuple<VertexBuffer, IndexBuffer, int>(null, null, 0);
                }

                GDVertices = new VertexBuffer(gd, typeof(VertexPositionColor), vertCount, BufferUsage.None);
                Dirty = true;

                GDIndices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Indices.Count, BufferUsage.None);
                GDIndices.SetData(Indices.ToArray());

                PrimitiveCount = Indices.Count / 3;
            }

            if (Dirty)
            {
                GDVertices.SetData(BatchProcess());
                Dirty = false;
            }

            return new Tuple<VertexBuffer, IndexBuffer, int>(GDVertices, GDIndices, PrimitiveCount);
        }

        public void Dispose()
        {
            GDIndices?.Dispose();
            GDVertices?.Dispose();

            GDIndices = null;
            GDVertices = null;
        }
    }
}
