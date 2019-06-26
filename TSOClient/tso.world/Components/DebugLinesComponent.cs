using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    public class DebugLinesComponent : IDisposable
    {
        public Blueprint BP;
        public bool StencilMode = false;
        public int Level;

        public List<VertexPositionColor> Vertices = new List<VertexPositionColor>();
        public List<int> Indices = new List<int>();

        public VertexBuffer VertBuffer;
        public IndexBuffer IndBuffer;
        public int PrimCount;

        public object Owner;

        public DebugLinesComponent(Blueprint bp)
        {
            BP = bp;
        }

        public void Reset()
        {
            VertBuffer?.Dispose();
            IndBuffer?.Dispose();
            VertBuffer = null;
            IndBuffer = null;

            Vertices.Clear();
            Indices.Clear();
        }

        public void AddRectangle(Rectangle rect, Color color)
        {
            AddPath(new List<Vector2>()
            {
                rect.Location.ToVector2()/16f,
                (rect.Location + new Point(rect.Width, 0)).ToVector2()/16f,
                (rect.Location + rect.Size).ToVector2()/16f,
                (rect.Location + new Point(0, rect.Height)).ToVector2()/16f,
                rect.Location.ToVector2()/16f,
            }, color);
        }

        public void AddPath(List<Vector2> points, Color color)
        {
            for (int i=1; i<points.Count; i++)
            {
                AddLine(points[i - 1], points[i], color);
            }
        }

        public void AddLine(Vector2 from, Vector2 to, Color color)
        {
            var from3 = new Vector3(from.X, BP.InterpAltitude(new Vector3(from, 0)) + 0.1f, from.Y);
            var to3 = new Vector3(to.X, BP.InterpAltitude(new Vector3(to, 0)) + 0.1f, to.Y);

            AddLine(from3, to3, color);
        }

        private void AddLine(Vector3 from, Vector3 to, Color color)
        {
            //add geometry

            if (!StencilMode)
            {
                Indices.Add(Vertices.Count);
                Vertices.Add(new VertexPositionColor(from, color));
                Indices.Add(Vertices.Count);
                Vertices.Add(new VertexPositionColor(to, color));
            }

            /*
            var norm = from - to;
            norm.Normalize();
            var cross = Vector3.Cross(Vector3.Up, norm);
            cross.Normalize();
            */

        }

        public void Draw(GraphicsDevice gd, WorldState state)
        {
            if (VertBuffer == null)
            {
                if (Vertices.Count == 0) return;
                //upload
                VertBuffer = new VertexBuffer(gd, typeof(VertexPositionColor), Vertices.Count, BufferUsage.None);
                VertBuffer.SetData(Vertices.ToArray());
                IndBuffer = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, Indices.Count, BufferUsage.None);
                IndBuffer.SetData(Indices.ToArray());
                PrimCount = Indices.Count / 2;
            }

            var effect = WorldContent.GetBE(gd);

            effect.LightingEnabled = false;
            effect.Alpha = 1;
            effect.DiffuseColor = Vector3.One;
            effect.AmbientLightColor = Vector3.One;
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;

            //var view = view;state.Camera.View;
            var view = state.Camera.View;
            var projection = state.Camera.Projection;
            effect.View = view;
            effect.Projection = projection;// (state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection;
            effect.World = Matrix.CreateTranslation(0, (Level-1)*2.95f, 0) * Matrix.CreateScale(3f);
            gd.DepthStencilState = DepthStencilState.Default;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.AlphaBlend;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.Indices = IndBuffer;
                gd.SetVertexBuffer(VertBuffer);

                gd.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, PrimCount);
            }

            gd.DepthStencilState = DepthStencilState.Default;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
