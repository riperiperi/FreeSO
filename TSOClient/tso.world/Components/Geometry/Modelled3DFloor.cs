using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace FSO.LotView.Components.Geometry
{
    /// <summary>
    /// This class handles a 3D floor group which is represented by a model, rather than simple textured tiles.
    /// This is built up by projecting vertices from the model onto the floor geometry on the Y axis.
    /// </summary>
    public class Modelled3DFloor : FloorTileGroup
    {
        public VertexBuffer VertGPUData;
        public Modelled3DFloorTile TargetTile;
        public Blueprint Bp;
        public int PrimitiveCount;

        protected TerrainParallaxVertex[] Vertices;
        protected int[] Indices;

        public Modelled3DFloor(Blueprint bp)
        {
            Bp = bp;
        }

        public override void PrepareGPU(GraphicsDevice gd)
        {
            if (GPUData != null) GPUData.Dispose();
            if (VertGPUData != null) VertGPUData.Dispose();

            GenerateGeometry();

            GPUData = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Indices.Length, BufferUsage.None);
            GPUData.SetData(Indices);

            VertGPUData = new VertexBuffer(gd, typeof(TerrainParallaxVertex), Vertices.Length, BufferUsage.None);
            VertGPUData.SetData(Vertices);

            PrimitiveCount = Indices.Length / 3;
        }

        protected virtual void GenerateGeometry()
        {
            var verts = new List<TerrainParallaxVertex>();
            var inds = new List<int>();
            var indices = GeomForOffset.Keys;

            foreach (var index in indices)
            {
                AppendTile(inds, verts, TargetTile, index);
            }
            Vertices = verts.ToArray();
            Indices = inds.ToArray();
        }

        protected void AppendTile(List<int> inds, List<TerrainParallaxVertex> verts, Modelled3DFloorTile tile, ushort index)
        {
            var tileBaseX = index % Bp.Width;
            var tileBaseY = index / Bp.Width;
            var baseInd = verts.Count;

            var srcInds = tile.Indices;
            var indLength = srcInds.Length;
            for (int i=0; i<indLength; i++)
            {
                inds.Add(srcInds[i] + baseInd);
            }

            var baseX = (int)Math.Max(1, Math.Min(Bp.Width - 1, tileBaseX));
            var baseY = (int)Math.Max(1, Math.Min(Bp.Height - 1, tileBaseY));
            var nextX = (int)Math.Max(1, Math.Min(Bp.Width - 1, tileBaseX + 1));
            var nextY = (int)Math.Max(1, Math.Min(Bp.Height - 1, tileBaseY + 1));

            var Altitude = Bp.Altitude;

            var by = (baseY % Bp.Height) * Bp.Width;
            var bx = (baseX % Bp.Width);
            var ny = (nextY % Bp.Height) * Bp.Width;
            var nx = (nextX % Bp.Width);
            float h00 = Altitude[(by + bx)];
            float h01 = Altitude[(ny + bx)];
            float h10 = Altitude[(by + nx)];
            float h11 = Altitude[(ny + nx)];
            var tFactor = Bp.TerrainFactor;

            var srcVerts = tile.Vertices;
            for (int i=0; i<srcVerts.Length; i++)
            {
                var vert = srcVerts[i];

                var xLerp = vert.Position.X;
                var yLerp = vert.Position.Z;
                float xl1 = xLerp * h10 + (1 - xLerp) * h00;
                float xl2 = xLerp * h11 + (1 - xLerp) * h01;

                var yOff = (yLerp * xl2 + (1 - yLerp) * xl1) * tFactor;

                vert.Position = (vert.Position + new Microsoft.Xna.Framework.Vector3(tileBaseX, yOff, tileBaseY)) * 3f;
                verts.Add(vert);
            }
        }

        public virtual Texture2D GetTexture(GraphicsDevice gd)
        {
            return TargetTile?.GetTexture(gd);
        }

        public override void Dispose()
        {
            base.Dispose();
            VertGPUData.Dispose();
        }
    }
}
