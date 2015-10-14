using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.UI
{
    public class BHAVContainer : UIContainer
    {
        public List<PrimitiveBox> Primitives;
        public Dictionary<byte, PrimitiveBox> PrimByID;

        public void CleanPosition()
        {
            var notTraversed = new HashSet<PrimitiveBox>(Primitives);
            while (notTraversed.Count > 0)
            {
                var instructionTree = new List<List<PrimitiveBox>>();

                recurseTree(notTraversed, instructionTree, notTraversed.ElementAt(0), 0);

                int treeWidth = 0;
                foreach (var row in instructionTree)
                {
                    int rowWidth = -20;
                    foreach (var inst in row)
                    {
                        rowWidth += inst.Width + 20;
                    }
                    int startPos = rowWidth / -2;
                    foreach (var inst in row)
                    {
                        rowWidth += inst.Width + 20;
                    }
                    if (rowWidth > treeWidth) treeWidth = rowWidth;
                    startPos.Y += 32;

                }
            }
        }

        private void recurseTree(HashSet<PrimitiveBox> notTraversed, List<List<PrimitiveBox>> instructionTree, PrimitiveBox primUI, byte depth)
        {
            if (primUI == null || !notTraversed.Contains(primUI)) return;

            while (depth >= instructionTree.Count) instructionTree.Add(new List<PrimitiveBox>());
            instructionTree[depth].Add(primUI);
            notTraversed.Remove(primUI);

            //traverse false then true branch
            recurseTree(notTraversed, instructionTree, primUI.TrueUI, (byte)(depth + 1));
            recurseTree(notTraversed, instructionTree, primUI.FalseUI, (byte)(depth + 1));
        }


        public override void Draw(UISpriteBatch batch)
        {
            foreach (var child in Primitives)
            {
                child.ShadDraw(batch);
            }

            base.Draw(batch);
        }
    }
}
