using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.IDE.EditorComponent.UI
{
    public class BHAVContainer : UIContainer
    {
        public List<PrimitiveBox> Primitives;
        public List<PrimitiveBox> RealPrim;
        public List<PrimitiveBox> Selected;
        public PrimitiveBox DebugPointer;

        public int Width;
        public int Height;

        public EditorScope Scope;
        public BHAV EditTarget;
        public TREE EditTargetTree;
        public UIBHAVEditor Editor {
            get
            {
                return (UIBHAVEditor)Parent;
            }
        }
        public event BHAVPrimSelect OnSelectedChanged;

        public PrimitiveBox HoverPrim;

        private bool m_doDrag;
        private float m_dragOffsetX;
        private float m_dragOffsetY;
        private UIMouseEventRef HitTest;

        public float AnimScrollX
        {
            set
            {
                X = value;
                ForceRedraw = true;
            }
            get
            {
                return X;
            }
        }

        public float AnimScrollY
        {
            set
            {
                Y = value;
                ForceRedraw = true;
            }
            get
            {
                return Y;
            }
        }

        private int lastWidth;
        private int lastHeight;

        public bool ForceRedraw;

        private void DragMouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    state.InputManager.SetFocus(null);
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

        public void ClearSelection()
        {
            Selected.Clear();
            if (OnSelectedChanged != null) OnSelectedChanged(Selected);
        }

        public BHAVInstruction GetInstruction(byte pointer)
        {
            if (pointer >= EditTarget.Instructions.Length) return null;
            return EditTarget.Instructions[pointer];
        }

        public void Init()
        {
            Selected = new List<PrimitiveBox>();
            Primitives = new List<PrimitiveBox>();
            RealPrim = new List<PrimitiveBox>();

            var childCopy = GetChildren().ToList();
            foreach (var child in childCopy) Remove(child);

            HoverPrim = null;

            foreach (var box in EditTargetTree.Entries)
            {
                var ui = new PrimitiveBox(box, this);
                Primitives.Add(ui);
                if (box.Type == TREEBoxType.Primitive) RealPrim.Add(ui);
                this.Add(ui);
            }

            foreach (var prim in Primitives)
            {
                var box = prim.TreeBox;
                if (box.TruePointer != -1)
                {
                    prim.TrueUI = Primitives[box.TruePointer];
                }
                if (box.FalsePointer != -1)
                {
                    prim.FalseUI = Primitives[box.FalsePointer];
                }
            }

            CleanPosition();
        }

        public BHAVContainer(BHAV target, EditorScope scope)
        {
            Scope = scope;
            EditTarget = target;
            EditTargetTree = scope.ActiveTree;

            Init();
            HitTest = ListenForMouse(new Rectangle(Int32.MinValue / 2, Int32.MinValue / 2, Int32.MaxValue, Int32.MaxValue), new UIMouseEvent(DragMouseEvents));
        }

        public void AddPrimitive(PrimitiveBox prim)
        {
            Primitives.Add(prim);
            RealPrim.Add(prim);
            this.Add(prim);
        }

        public void RemovePrimitive(PrimitiveBox prim)
        {
            if (prim.TreeBox.InternalID != -1)
            {
                EditTargetTree.DeleteBox(prim.TreeBox);
            }
            Primitives.Remove(prim);
            RealPrim.RemoveAt(prim.InstPtr);
            this.Remove(prim);
        }

        public void UpdateLabelPointers(short index)
        {
            UpdateLabelPointers(index, new HashSet<short>());
        }

        public void UpdateLabelPointers(short index, HashSet<short> traversed)
        {
            //if we've already traversed this label, we're in a loop and don't need to update our pointers
            //should probably show a warning telling the user not to do this
            if (traversed.Contains(index)) return;

            traversed.Add(index);
            foreach (var prim in Primitives)
            {
                if (prim.Type == TREEBoxType.Goto && prim.TreeBox.TruePointer == index)
                {
                    //update bhav instructions of all primitives pointing to this goto
                    foreach (var prim2 in Primitives)
                    {
                        if (prim2.TrueUI == prim)
                        {
                            if (prim2.Type == TREEBoxType.Label)
                            {
                                //we need to update this label too
                                UpdateLabelPointers(prim2.TreeBox.InternalID, traversed);
                            }
                            else if (prim2.Type == TREEBoxType.Primitive)
                            {
                                prim2.Instruction.TruePointer = prim.InstPtr;
                            }
                        }
                        if (prim2.FalseUI == prim)
                        {
                            if (prim2.Type == TREEBoxType.Primitive)
                            {
                                prim2.Instruction.FalsePointer = prim.InstPtr;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateOperand(PrimitiveBox target)
        {
            ForceRedraw = true;

            target.Descriptor.Operand.Write(target.Instruction.Operand);
            FSO.SimAntics.VM.BHAVChanged(EditTarget);
            target.UpdateDisplay();
        }

        public void CleanPosition()
        {
            var notTraversed = new HashSet<PrimitiveBox>(Primitives.Where(x => x.TreeBox.PosisionInvalid));
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
                        inst.TreeBox.PosisionInvalid = false;
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
                    ui.CopyPosToTree();
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
            DrawTiledTexture(batch, res.Background, new Rectangle((int)Math.Floor(this.Position.X/-200)*200, (int)Math.Floor(this.Position.Y/ -200)*200, Width+200, Height+200), Color.White);

            foreach (var child in Primitives)
            {
                child.ShadDraw(batch);
            }

            foreach (var child in Primitives)
            {
                child.NodeDraw(batch);
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
        }
        
    }

    public delegate void BHAVPrimSelect(List<PrimitiveBox> selected);
}
