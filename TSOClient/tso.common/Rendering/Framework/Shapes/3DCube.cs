using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Common.Rendering.Framework.Shapes
{
    public class _3DCube : _3DComponent
    {
        private BasicEffect Effect;

        private VertexPositionColor[] Geom;
        private List<VertexPositionColor> GeomList;

        private Color Color;
        private Vector3 Size;

        public _3DCube(Color color, Vector3 size)
        {
            this.Color = color;
            this.Size = size;
        }

        public override void Initialize(){
            
            Effect = new BasicEffect(Device);

            /** Bottom Face **/
            var btmTL = new Vector3(0.0f, 0.0f, 0.0f);
            var btmTR = new Vector3(Size.X, 0.0f, 0.0f);
            var btmBR = new Vector3(Size.X, 0.0f, Size.Z);
            var btmBL = new Vector3(0.0f, 0.0f, Size.Z);

            /** Top face **/
            var topTL = new Vector3(0.0f, Size.Y, 0.0f);
            var topTR = new Vector3(Size.X, Size.Y, 0.0f);
            var topBR = new Vector3(Size.X, Size.Y, Size.Z);
            var topBL = new Vector3(0.0f, Size.Y, Size.Z);


            GeomList = new List<VertexPositionColor>();
            AddQuad(Color, topTL, topTR, topBR, topBL);
            AddQuad(Color.Yellow, btmTL, btmTR, btmBR, btmBL);
            AddQuad(Color.Green, topTL, topTR, btmTR, btmTL);
            AddQuad(Color.Blue, topBL, topTL, btmTL, btmBL);
            AddQuad(Color.Orange, topBR, topTR, btmTR, btmBR);
            AddQuad(Color.White, topBL, topBR, btmBR, btmBL);

            Geom = GeomList.ToArray();
        }

        private void AddQuad(Color color, Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl)
        {
            GeomList.Add(new VertexPositionColor(tl, color));
            GeomList.Add(new VertexPositionColor(tr, color));
            GeomList.Add(new VertexPositionColor(br, color));

            GeomList.Add(new VertexPositionColor(br, color));
            GeomList.Add(new VertexPositionColor(bl, color));
            GeomList.Add(new VertexPositionColor(tl, color));
        }

        public override void Draw(GraphicsDevice device)
        {

            Effect.World = World;
            Effect.View = View;
            Effect.Projection = Projection;
            Effect.VertexColorEnabled = true;
            //Effect.EnableDefaultLighting();

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, Geom, 0, Geom.Length / 3);
            }
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
        }
    }
}
