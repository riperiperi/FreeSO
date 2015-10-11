using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView;
using FSO.SimAntics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class BHAVTree
    {
        private List<List<byte>> InstructionTree;
        private BHAVInstPosition[] InstPos;
        private BHAV MyBHAV;

        private VMContext context; //hack to get some names right noe

        private Texture2D whitePx;

        public BHAVTree(BHAV input)
        {
            MyBHAV = input;
            InstructionTree = new List<List<byte>>();
            var notTraversed = new HashSet<BHAVInstruction>(input.Instructions);

            context = new VMContext((World)null);
            recurseTree(notTraversed, 0, 0);

            InstPos = new BHAVInstPosition[input.Instructions.Length];
            var startPos = new Vector2(320, 16);
            foreach (var row in InstructionTree)
            {
                var leftOffset = new Vector2(-75, 0) * (row.Count - 1);
                var indentOffset = new Vector2(150, 0);
                int i = 0;
                foreach (var inst in row)
                {
                    InstPos[inst] = new BHAVInstPosition {
                        Pos = startPos + leftOffset + indentOffset * (i++)
                    };
                }
                startPos.Y += 32;
            }
        }


        public void InitResource(GraphicsDevice gd)
        {
            whitePx = new Texture2D(gd, 1, 1);
            whitePx.SetData<Color>(new Color[] { new Color(64, 64, 64, 128) });
        }

        private void recurseTree(HashSet<BHAVInstruction> notTraversed, byte instPtr, byte depth)
        {
            if (instPtr >= 253) return;
            var inst = MyBHAV.Instructions[instPtr];
            if (!notTraversed.Contains(inst)) return;

            while (depth >= InstructionTree.Count) InstructionTree.Add(new List<byte>());
            InstructionTree[depth].Add(instPtr);
            notTraversed.Remove(inst);

            //traverse false then true branch
            recurseTree(notTraversed, inst.FalsePointer, (byte)(depth + 1));
            recurseTree(notTraversed, inst.TruePointer, (byte)(depth + 1));
        }

        public void Draw(SpriteBatch batch, SpriteFont font)
        {
            for (int i=0; i<MyBHAV.Instructions.Length; i++)
            {
                var inst = MyBHAV.Instructions[i];
                var pos = InstPos[i];
                if (pos == null) continue;

                if (inst.FalsePointer < 253)
                {
                    var destPos = InstPos[inst.FalsePointer];
                    DrawLine(whitePx, pos.Pos, destPos.Pos, batch, 2, 1);
                }

                if (inst.TruePointer < 253)
                {
                    var destPos = InstPos[inst.TruePointer];
                    DrawLine(whitePx, pos.Pos, destPos.Pos, batch, 2, 1);
                }

                var name = (inst.Opcode > 255) ? ("Call to BHAV " + inst.Opcode) : context.Primitives[inst.Opcode].Name;
                var measure = font.MeasureString(name);
                batch.DrawString(font, name, pos.Pos - measure / 2, Color.Blue);
            }
        }

        private void DrawLine(Texture2D Fill, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth, float opacity) //draws a line from Start to End.
        {
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            Color tint = new Color(1f, 1f, 1f, 1f) * opacity;
            spriteBatch.Draw(Fill, new Rectangle((int)Start.X, (int)Start.Y - (int)(lineWidth / 2), (int)length, lineWidth), null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); //
        }
    }

    public class BHAVInstPosition {
        public Vector2 Pos;
    }
}
