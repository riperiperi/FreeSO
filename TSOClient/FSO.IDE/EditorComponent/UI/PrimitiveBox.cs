using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.UI
{
    public class PrimitiveBox : UIContainer
    {
        private int _Width = 212;
        public int Width
        {
            get { return _Width; }
            set { _Width = value; }
        }

        private int _Height = 67;
        public int Height
        {
            get { return _Height; }
            set { _Height = value; }
        }

        private static Color ShadCol = new Color(0xAF, 0xAF, 0xA3);

        public byte InstPtr;
        public BHAVInstruction Instruction;

        public PrimitiveStyle Style;
        public bool Dead = false;
        private PrimitiveNode[] Nodes;

        public PrimitiveBox FalseUI
        {
            get { return Nodes[0].Destination; }
        }
        public PrimitiveBox TrueUI
        {
            get { return Nodes[1].Destination; }
        }

        public PrimitiveBox()
        {
            Nodes = new PrimitiveNode[2];
            Nodes[0] = new PrimitiveNode();
            Nodes[0].Type = NodeType.False;
            Nodes[1] = new PrimitiveNode();
            Nodes[1].Type = NodeType.True;
        }

        public void ShadDraw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(Width, Height), ShadCol);
            foreach (var child in Nodes)
            {
                child.ShadDraw(batch);
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(Width, Height)); //white outline
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1,1), new Vector2(Width-2, Height-2), Style.Background); //background
            DrawTiledTexture(batch, res.DiagTile, new Rectangle(1, 1, Width - 2, Height - 2), new Color(Color.White, Style.DiagBrightness));
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1, 1), new Vector2(Width - 2, 20), new Color(Color.White, 0.66f)); //title bg

            base.Draw(batch);
        }

        public void UpdateNodePos()
        {
            //we want to put nodes on the side closest to the destination. For this we use a vector from this node to the closest point on the destination.
            //to avoid crossover the side lists should be ordered by Y position.

            var dirNodes = new List<PrimitiveNode>[4];  
            for (int i = 0; i < 4; i++) dirNodes[i] = new List<PrimitiveNode>();
            //0 = down, 1 = left, 2 = up, 3 = right

            for (int i = 0; i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                var centerPos = Position + new Vector2(Width / 2, Height / 2);
                var vec = ((node.Destination == null)?centerPos:node.Destination.NearestDestPt(centerPos)) - centerPos;

                if (Math.Abs(vec.X) > Math.Abs(vec.Y)) {
                    //horizontal
                    var dest = (vec.X > 0) ? 3 : 1;
                    var list = dirNodes[dest];
                    //insert in list in order of lowest y first
                    bool inserted = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        var elem = list[j];
                        if (vec.Y < elem.Y)
                        {
                            list.Insert(j, node);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted) list.Add(node);
                    node.Y = vec.Y; //temporary storage for sorting, since it'll be refreshed later.
                }
                else
                {
                    //vertical
                    var dest = (vec.Y > 0) ? 0 : 2;
                    var list = dirNodes[dest];
                    //insert in list in order of lowest x first
                    bool inserted = false;
                    for (int j = 0; j<list.Count; j++)
                    {
                        var elem = list[j];
                        if (vec.X < elem.X)
                        {
                            list.Insert(j, node);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted) list.Add(node);
                    node.X = vec.X; //temporary storage for sorting, since it'll be refreshed later.
                }
            }

            for (int i=0; i<4; i++)
            {
                var bStart = 0;
                var bSize = (i % 2 == 0) ? Height : Width;
                var list = dirNodes[i];
                bSize /= list.Count+2;
                bStart += bSize;
                foreach (var node in list)
                {
                    node.Direction = i;
                    if (i % 2 == 0)
                        node.Position = new Vector2(bStart, (i == 0) ? Height+6 : -6);
                    else
                        node.Position = new Vector2(bStart, (i == 3) ? Width+6 : -6);
                }
            }
        }

        public Vector2 NearestDestPt(Vector2 pt)
        {
            return new Vector2(Math.Min(Math.Max(X, pt.X), X + Width), Math.Min(Math.Max(Y, pt.Y), Y + Height));
        }
    }
}
