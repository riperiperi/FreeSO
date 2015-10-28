using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using Microsoft.Xna.Framework;
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
        public List<PrimitiveBox> Selected;

        public EditorScope Scope;
        public BHAV EditTarget;
        public event BHAVPrimSelect OnSelectedChanged;

        public PrimitiveBox HoverPrim;

        private bool m_doDrag;
        private float m_dragOffsetX;
        private float m_dragOffsetY;
        private UIMouseEventRef HitTest;

        private int lastWidth;
        private int lastHeight;

        private bool ForceRedraw;

        private void DragMouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    m_doDrag = true;
                    var position = this.GetMousePosition(state.MouseState);
                    m_dragOffsetX = position.X;
                    m_dragOffsetY = position.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    m_doDrag = false; //should probably just release when mouse is up in any case.
                    break;
            }
        }

        public void Select(PrimitiveBox box)
        {
            Selected.Clear();
            Selected.Add(box);
            if (OnSelectedChanged != null) OnSelectedChanged(Selected);
        }

        public BHAVContainer(BHAV target, EditorScope scope)
        {
            Scope = scope;
            EditTarget = target;

            Selected = new List<PrimitiveBox>();
            Primitives = new List<PrimitiveBox>();
            PrimByID = new Dictionary<byte, PrimitiveBox>();

            byte i = 0;
            foreach (var inst in EditTarget.Instructions)
            {
                var ui = new PrimitiveBox(inst, i++, this);
                Primitives.Add(ui);
                PrimByID.Add(ui.InstPtr, ui);
                this.Add(ui);
            }

            var RealPrims = new List<PrimitiveBox>(Primitives);
            foreach (var prim in RealPrims)
            {
                if (prim.Instruction.FalsePointer > 252 && prim.Returns != PrimitiveReturnTypes.Done)
                {
                    var dest = new PrimitiveBox((prim.Instruction.FalsePointer == 254) ? PrimBoxType.True : PrimBoxType.False, this);
                    Primitives.Add(dest);
                    this.Add(dest);
                    prim.FalseUI = dest;
                }
                else if (PrimByID.ContainsKey(prim.Instruction.FalsePointer)) prim.FalseUI = PrimByID[prim.Instruction.FalsePointer];

                if (prim.Instruction.TruePointer > 252)
                {
                    var dest = new PrimitiveBox((prim.Instruction.TruePointer == 254) ? PrimBoxType.True : PrimBoxType.False, this);
                    Primitives.Add(dest);
                    this.Add(dest);
                    prim.TrueUI = dest;
                }
                else if (PrimByID.ContainsKey(prim.Instruction.TruePointer)) prim.TrueUI = PrimByID[prim.Instruction.TruePointer];
            }
            CleanPosition();

            HitTest = ListenForMouse(new Rectangle(Int32.MinValue/2, Int32.MinValue / 2, Int32.MaxValue, Int32.MaxValue), new UIMouseEvent(DragMouseEvents));
        }

        public void UpdateOperand(PrimitiveBox target)
        {
            ForceRedraw = true;

            target.Descriptor.Operand.Write(target.Instruction.Operand);
            FSO.SimAntics.VM.BHAVChanged(EditTarget);
            target.UpdateDisplay();

            /*
            foreach (var prim in Primitives)
            {
                if (prim.Descriptor != null)
                {
                    prim.UpdateDisplay();
                }
            }*/
        }

        public void CleanPosition()
        {
            var notTraversed = new HashSet<PrimitiveBox>(Primitives);
            int xOff = 0;
            while (notTraversed.Count > 0)
            {
                var instructionTree = new List<List<PrimitiveBox>>();

                recurseTree(notTraversed, instructionTree, notTraversed.ElementAt(0), 0);

                int treeWidth = 0;
                int yPos = 0;
                var treePrims = new List<PrimitiveBox>();
                foreach (var row in instructionTree)
                {
                    int rowWidth = -20;
                    foreach (var inst in row)
                    {
                        rowWidth += inst.Width + 45;
                    }
                    int xPos = rowWidth / -2;
                    int maxHeight = 0;
                    foreach (var inst in row)
                    {
                        treePrims.Add(inst);
                        inst.Position = new Vector2(xPos, yPos);
                        if (inst.Height > maxHeight) maxHeight = inst.Height;
                        xPos += inst.Width + 45;
                    }
                    yPos += 45 + maxHeight;
                    if (rowWidth > treeWidth) treeWidth = rowWidth;
                }

                int halfWidth = treeWidth / 2;
                foreach (var ui in treePrims)
                {
                    ui.Position = new Vector2(ui.X + xOff + halfWidth + 20, ui.Y);
                }
                xOff += treeWidth + 60;
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

        public static double PosMod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public override void Draw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            DrawTiledTexture(batch, res.Background, new Rectangle((int)Math.Floor(this.Position.X/-200)*200, (int)Math.Floor(this.Position.Y/ -200)*200, batch.Width+200, batch.Height+200), Color.White);

            foreach (var child in Primitives)
            {
                child.ShadDraw(batch);
            }

            base.Draw(batch);
        }

        public override void Update(UpdateState state)
        {
            lastWidth = state.UIState.Width;
            lastHeight = state.UIState.Height;
            if (m_doDrag)
            {
                var position = Parent.GetMousePosition(state.MouseState);
                state.SharedData["ExternalDraw"] = true;
                this.X = position.X - m_dragOffsetX;
                this.Y = position.Y - m_dragOffsetY;
            }
            base.Update(state);

            if (ForceRedraw)
            {
                state.SharedData["ExternalDraw"] = true;
                ForceRedraw = false;
            }
            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Q)) Parent.Remove(this);
        }
        
    }

    public delegate void BHAVPrimSelect(List<PrimitiveBox> selected);
}
