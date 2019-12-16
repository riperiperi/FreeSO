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
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.SimAntics.Engine;
using FSO.IDE.EditorComponent.Primitives;
using FSO.IDE.EditorComponent.Commands;
using Microsoft.Xna.Framework.Input;

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

        public double CenX
        {
            get => X + (Width / 2);                    
        }

        public double CenY
        {
            get => Y + (Height / 2);
        }

        public override Vector2 Size
        {
            get
            {
                return new Vector2(Width, Height);
            }

            set
            {

            }
        }

        private static Color ShadCol = new Color(0xAF, 0xAF, 0xA3);

        
        public BHAVInstruction Instruction;
        public BHAVContainer Master;
        public TREEBox TreeBox;

        public byte InstPtr => TreeBox.TrueID;
        public TREEBoxType Type => TreeBox.Type;
        public bool Untargetable => Type == TREEBoxType.Label || Type == TREEBoxType.Comment;
        public bool Resizable => Type == TREEBoxType.Comment;

        public PrimitiveDescriptor Descriptor;
        private VMPrimitiveOperand Operand;
        public PrimitiveStyle Style;
        public bool Dead = false;
        private PrimitiveNode[] Nodes;
        public PrimitiveReturnTypes Returns
        {
            get { return (Descriptor == null)?PrimitiveReturnTypes.TrueFalse:Descriptor.Returns; }
        }

        private int DoubleClickTime = 0;
        private UIImage SliceBg;
        private CommentContainer CommentNode;
        private UILabel Title;
        private UILabel Index;
        private UITextEdit TextEdit;
        public string TitleText;
        public string BodyText;
        private TextRendererResult BodyTextLabels;
        private TextStyle BodyTextStyle;

        private UIMouseEventRef HitTest;

        public PrimitiveBox FalseUI
        {
            get { return (Nodes.Length > 0) ? Nodes[0].Destination : null; }
            set { if (Nodes.Length > 0) Nodes[0].Destination = value; }
        }
        public PrimitiveBox TrueUI
        {
            get { return (Nodes.Length > 1) ? Nodes[1].Destination : null; }
            set { if (Nodes.Length > 1) Nodes[1].Destination = value; }
        }

        public PrimitiveBox(BHAVInstruction inst, BHAVContainer master)
        {
            TreeBox = new TREEBox(null);
            Master = master;
            Instruction = inst;
            HitTest = ListenForMouse(new Rectangle(0, 0, Width, Height), new UIMouseEvent(MouseEvents));
            PreparePrimitive();
        }

        public PrimitiveBox(TREEBox box, BHAVContainer master)
        {
            TreeBox = box;
            Master = master;
            Nodes = new PrimitiveNode[0];
            ApplyBoxPosition();
            HitTest = ListenForMouse(new Rectangle(0, 0, Width, Height), new UIMouseEvent(MouseEvents));
            Texture2D sliceTex = null;
            switch (Type)
            {
                case TREEBoxType.Primitive:
                    Instruction = master.GetInstruction(box.TrueID);
                    PreparePrimitive();
                    break;
                case TREEBoxType.True:
                case TREEBoxType.False:
                    RecenterSize(32, 32);
                    break;
                case TREEBoxType.Label:
                    sliceTex = EditorResource.Get().LabelBox;
                    Nodes = new PrimitiveNode[2];
                    Nodes[0] = new PrimitiveNode();
                    Nodes[0].Visible = false;
                    Nodes[1] = new PrimitiveNode();
                    Nodes[1].Type = NodeType.Done;
                    this.Add(Nodes[0]);
                    this.Add(Nodes[1]);

                    TextEdit = new UITextEdit();
                    TextEdit.OnChange += (elem) => { CommentChanged(); };
                    TextEdit.OnFocusOut += TextEdit_OnFocusOut;
                    TextEdit.TextStyle = EditorResource.Get().TitleStyle;
                    TextEdit.Alignment = TextAlignment.Center;
                    TextEdit.CurrentText = TreeBox.Comment;
                    TextEdit.NoFocusPassthrough += MouseEvents;
                    Add(TextEdit);
                    CommentResized();
                    break;
                case TREEBoxType.Goto:
                    sliceTex = EditorResource.Get().GotoBox;

                    Title = new UILabel();
                    Title.Alignment = TextAlignment.Middle | TextAlignment.Center;
                    Title.Y = 0;
                    Title.X = 0;
                    this.Add(Title);
                    Title.CaptionStyle = EditorResource.Get().TitleStyle;

                    UpdateGotoLabel();
                    break;
                case TREEBoxType.Comment:
                    sliceTex = EditorResource.Get().CommentBox;
                    TextEdit = new UITextEdit();
                    TextEdit.OnChange += (elem) => { CommentChanged(); };
                    TextEdit.OnFocusOut += TextEdit_OnFocusOut;
                    TextEdit.TextStyle = EditorResource.Get().CommentStyle;
                    TextEdit.CurrentText = TreeBox.Comment;
                    TextEdit.NoFocusPassthrough += MouseEvents;
                    Add(TextEdit);
                    CommentResized();
                    break;
            }
            if (sliceTex != null)
            {
                var sliceW = sliceTex.Width / 3;
                var sliceH = sliceTex.Height / 3;
                SliceBg = new UIImage(sliceTex).With9Slice(sliceW, sliceW, sliceH, sliceH);
                SliceBg.Width = Width;
                SliceBg.Height = Height;
                Add(SliceBg);
            }
        }

        private void TextEdit_OnFocusOut(UIElement element)
        {
            if (TreeBox.Comment != TextEdit.CurrentText)
            {
                Master.Editor.QueueCommand(new CommentModifyCommand(this, TextEdit.CurrentText));
            }
        }

        private void ApplyBoxPosition()
        {
            Position = new Vector2(TreeBox.X, TreeBox.Y);
            Width = TreeBox.Width;
            Height = TreeBox.Height;
        }

        public void ApplyBoxPositionCentered()
        {
            ApplyBoxPosition();
            switch (Type)
            {
                case TREEBoxType.Primitive:
                    UpdateDisplay();
                    break;
                case TREEBoxType.Goto:
                    UpdateGotoLabel();
                    break;
                case TREEBoxType.Label:
                case TREEBoxType.Comment:
                    CommentResized();
                    break;
            }
        }

        public void SetTreeBox(TREEBox box)
        {
            TreeBox = box;
        }

        public void PreparePrimitive()
        {
            Descriptor = PrimitiveRegistry.GetDescriptor(Instruction.Opcode);
            Operand = (VMPrimitiveOperand)Activator.CreateInstance(Descriptor.OperandType);
            Operand.Read(Instruction.Operand);

            Nodes = new PrimitiveNode[2];
            Nodes[0] = new PrimitiveNode();
            Nodes[0].Type = NodeType.False;
            Nodes[1] = new PrimitiveNode();
            Nodes[1].Type = NodeType.True;

            Title = new UILabel();
            Title.Alignment = TextAlignment.Middle | TextAlignment.Center;
            Title.Y = 0;
            Title.X = 0;
            this.Add(Title);
            Title.CaptionStyle = TextStyle.DefaultLabel.Clone();
            Title.CaptionStyle.Font = FSO.Client.GameFacade.EdithFont;
            Title.CaptionStyle.VFont = FSO.Client.GameFacade.EdithVectorFont;
            Title.CaptionStyle.Size = 14;

            Index = new UILabel();
            Index.Alignment = TextAlignment.Right | TextAlignment.Center;
            Index.Y = 0;
            Index.X = 0;
            this.Add(Index);
            Index.CaptionStyle = TextStyle.DefaultLabel.Clone();
            Index.CaptionStyle.Font = FSO.Client.GameFacade.EdithFont;
            Index.CaptionStyle.VFont = FSO.Client.GameFacade.EdithVectorFont;
            Index.CaptionStyle.Size = 10;
            Index.Visible = false;

            BodyTextStyle = TextStyle.DefaultLabel.Clone();
            BodyTextStyle.Font = FSO.Client.GameFacade.EdithFont;
            BodyTextStyle.VFont = FSO.Client.GameFacade.EdithVectorFont;
            BodyTextStyle.Size = 12;

            CommentNode = new CommentContainer(TreeBox.Comment);
            CommentNode.OnCommentChanged += CommentNode_OnCommentChanged;
            CommentNode.Y = -3;
            Add(CommentNode);

            this.Add(Nodes[0]);
            this.Add(Nodes[1]);

            UpdateDisplay();
        }

        private void CommentNode_OnCommentChanged(string comment)
        {
            Master.Editor.QueueCommand(new CommentModifyCommand(this, comment));
        }

        public void RefreshOperand()
        {
            Operand.Read(Instruction.Operand);
        }

        private void RecenterSize(int width, int height)
        {
            var centerPos = Position + new Vector2(Width / 2, Height / 2);

            Width = width;
            Height = height;

            UpdateHitbox();

            Position = centerPos - new Vector2(Width, Height) / 2;
        }

        private void UpdateHitbox()
        {
            HitTest.Region.Width = Width;
            HitTest.Region.Height = Height;
            if (SliceBg != null)
            {
                SliceBg.Width = Width;
                SliceBg.Height = Height;
            }
        }

        public void SetComment(string comment)
        {
            TreeBox.Comment = comment;
            if (TextEdit != null && TextEdit.CurrentText != comment) TextEdit.CurrentText = comment;
            if (CommentNode != null && CommentNode.Comment != comment) CommentNode.SetComment(comment);
        }

        public void CommentChanged()
        {
            if (Type == TREEBoxType.Label)
            {
                CommentResized();
            }
        }

        public void CommentResized()
        {
            if (Type == TREEBoxType.Label)
            {
                //auto resize based on text edit contents
                RecenterSize((int)TextEdit.TextStyle.MeasureString(TextEdit.CurrentText).X + 26, 26);
            }

            //fit the text box to the new size.
            var margin = (Type == TREEBoxType.Comment) ? 8 : 4;
            TextEdit.SetSize(Width - margin*2, Height - margin * 2);
            TextEdit.Position = new Vector2(margin);
            if (Type == TREEBoxType.Comment) TextEdit.Y -= 3;
        }

        public void UpdateGotoLabel()
        {
            TitleText = TreeBox.Parent.GetBox(TreeBox.TruePointer)?.Comment ?? "(Missing Label)";
            var titleWidth = Title.CaptionStyle.MeasureString(TitleText).X;
            Title.Caption = TitleText;

            RecenterSize((int)titleWidth + 26, 26);
            Title.Size = new Vector2(Width, 27);
        }

        public void UpdateDisplay()
        {
            Descriptor.Operand = Operand;
            Style = PGroupStyles.ByType[Descriptor.Group];

            TitleText = Descriptor.GetTitle(Master.Scope);
            var titleWidth = Title.CaptionStyle.MeasureString(TitleText).X;
            Title.Caption = TitleText;
            Title.CaptionStyle.Color = Style.Title;
            Index.Caption = InstPtr.ToString();
            Index.CaptionStyle.Color = Style.Title;

            BodyText = Descriptor.GetBody(Master.Scope);
            BodyTextStyle.Color = Style.Body;

            BodyTextLabels = TextRenderer.ComputeText(BodyText, new TextRendererOptions
            {
                Alignment = TextAlignment.Center,
                MaxWidth = 300,
                Position = new Microsoft.Xna.Framework.Vector2(0, 24),
                Scale = _Scale,
                TextStyle = BodyTextStyle,
                WordWrap = true,
            }, this);

            RecenterSize(Math.Max((int)titleWidth, BodyTextLabels.MaxWidth) + 10, BodyTextLabels.BoundingBox.Height + 43);
            Title.Size = new Vector2(Width, 24);
            Index.Size = new Vector2(Width - 4, 20);

            var shift = (Width - 300) / 2;
            foreach (var cmd in BodyTextLabels.DrawingCommands)
            {
                if (cmd is TextDrawCmd_Text)
                {
                    ((TextDrawCmd_Text)cmd).Position += new Vector2(shift, 0);
                }
            }

            if (Descriptor.Returns == PrimitiveReturnTypes.TrueFalse)
            {
                Nodes[0].Visible = true;
                Nodes[1].Type = NodeType.True;
            }
            else
            {
                Nodes[0].Visible = false;
                Nodes[1].Type = NodeType.Done;
            }

            if (CommentNode != null)
            {
                CommentNode.X = Width + 3;
            }
        }

        private void DrawSliceShadow(UISpriteBatch batch, Color color, Vector2 offset)
        {
            var blend = SliceBg.BlendColor;
            color.A = (byte)(blend.A * (color.A / 255f));
            SliceBg.BlendColor = color;
            SliceBg.Position += offset;
            SliceBg.CalculateMatrix();
            SliceBg.Draw(batch);
            SliceBg.Position -= offset;
            SliceBg.CalculateMatrix();
            SliceBg.BlendColor = blend;
        }

        public void ShadDraw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            if (SliceBg != null)
            {
                DrawSliceShadow(batch, new Color(0, 0, 0, 51), new Vector2(5));
            }
            else if (Style == null || Style.Background.A > 200) DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(5,5), new Vector2(Width, Height), ShadCol);
            else DrawTiledTexture(batch, res.DiagTile, new Rectangle(5, 5, Width, Height), ShadCol);

            if (Type == TREEBoxType.Primitive)
            {
                int topInd = 0;
                if (Instruction.Breakpoint) DrawLocalTexture(batch, res.Breakpoint, null, new Vector2(-15, 6+((topInd++)*18)), new Vector2(1, 1), Color.Black * 0.2f);
                if (Master.DebugPointer == this) DrawLocalTexture(batch, res.CurrentArrow, null, new Vector2(-15, 6 + ((topInd++) * 18)), new Vector2(1, 1), Color.Black * 0.2f);
            }

            foreach (var child in Nodes)
            {
                child.ShadDraw(batch);
            }

            if (CommentNode != null)
            {
                CommentNode.ShadDraw(batch);
            }
        }

        public void NodeDraw(UISpriteBatch batch)
        {
            foreach (var child in Nodes)
            {
                child.Draw(batch);
            }
        }

        public override void CalculateMatrix()
        {
            base.CalculateMatrix();

            if (Type == TREEBoxType.Primitive)
            {
                BodyTextLabels = TextRenderer.ComputeText(BodyText, new TextRendererOptions
                {
                    Alignment = TextAlignment.Center,
                    MaxWidth = 300,
                    Position = new Microsoft.Xna.Framework.Vector2(0, 24),
                    Scale = _Scale,
                    TextStyle = BodyTextStyle,
                    WordWrap = true,
                }, this);

                var shift = (Width - 300) / 2;
                foreach (var cmd in BodyTextLabels.DrawingCommands)
                {
                    if (cmd is TextDrawCmd_Text)
                    {
                        ((TextDrawCmd_Text)cmd).Position += new Vector2(shift, 0);
                    }
                }
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            //base.Draw(batch);

            var res = EditorResource.Get();

            if (Type == TREEBoxType.True || Type == TREEBoxType.False)
            {
                DrawLocalTexture(batch, (Type == TREEBoxType.True) ? res.TrueReturn : res.FalseReturn, new Vector2());
            }
            else
            {
                if (InstPtr == 0)
                {
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(-3, -3), new Vector2(Width + 6, Height + 6), new Color(0x96, 0xFF, 0x73));
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(-2, -2), new Vector2(Width + 4, Height + 4), new Color(0x46, 0x8C, 0x00)); //start point green
                }

                if (SliceBg != null)
                {
                    if (Master.Selected.Contains(this))
                    {
                        DrawSliceShadow(batch, Color.Red, new Vector2(1));
                        DrawSliceShadow(batch, Color.Red, new Vector2(-1));
                        DrawSliceShadow(batch, Color.Red, new Vector2(1, -1));
                        DrawSliceShadow(batch, Color.Red, new Vector2(-1, 1));
                    }
                    SliceBg.Draw(batch);
                }
                else
                {
                    if (Style.Background.A > 200) DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(Width, Height), Master.Selected.Contains(this) ? Color.Red : Color.White); //white outline
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1, 1), new Vector2(Width - 2, Height - 2), Style.Background); //background
                    DrawTiledTexture(batch, res.DiagTile, new Rectangle(1, 1, Width - 2, Height - 2), Color.White * Style.DiagBrightness);
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1, 1), new Vector2(Width - 2, 20), Color.White * 0.66f); //title bg
                }

                Index?.Draw(batch);
                Title?.Draw(batch);
                TextEdit?.Draw(batch);
                if (BodyTextLabels != null) TextRenderer.DrawText(BodyTextLabels.DrawingCommands, this, batch);

                int topInd = 0;
                if (Instruction?.Breakpoint == true)
                    DrawLocalTexture(batch, res.Breakpoint, null, new Vector2(-20, 1 + ((topInd++) * 18)), new Vector2(1, 1), Color.White);
                if (Master.DebugPointer == this)
                    DrawLocalTexture(batch, res.CurrentArrow, null, new Vector2(-20, 1 + ((topInd++) * 18)), new Vector2(1, 1), Color.White);
            }

            if (CommentNode != null) CommentNode.Draw(batch);
        }


        private bool m_doDrag;
        private bool m_doResize;
        private float m_dragOffsetX;
        private float m_dragOffsetY;

        private void MouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseOver:
                    Master.HoverPrim = this;
                    break;
                case UIMouseEventType.MouseOut:
                    if (Master.HoverPrim == this) Master.HoverPrim = null;
                    break;
                case UIMouseEventType.MouseDown:
                    state.InputManager.SetFocus(null);
                    Master.Select(this);

                    if (DoubleClickTime > 0 && Type == TREEBoxType.Primitive && Descriptor is SubroutineDescriptor)
                    {
                        var subD = (SubroutineDescriptor)Descriptor;
                        FSO.Client.Debug.IDEHook.IDE.IDEOpenBHAV(Master.Scope.GetBHAV(subD.PrimID), Master.Scope.Object);
                    }
                    DoubleClickTime = 25;


                    m_doDrag = true;
                    var position = this.GetMousePosition(state.MouseState);
                    m_doResize = Resizable && position.X > Width - 10 && position.Y > Height - 10;
                    m_dragOffsetX = position.X;
                    m_dragOffsetY = position.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    m_doDrag = false; //should probably just release when mouse is up in any case.
                    Master.Editor.QueueCommand(new UpdateBoxPosCommand(this));
                    if (TextEdit != null) state.InputManager.SetFocus(TextEdit);
                    break;
                default:
                    break;
            }
            
            if ((evt == UIMouseEventType.MouseOut || evt == UIMouseEventType.MouseOver) && CommentNode != null)
            {
                CommentNode.MouseEvent(evt, state);
            }
        }

        public void CopyPosToTree()
        {
            TreeBox.X = (short)X;
            TreeBox.Y = (short)Y;
            TreeBox.Width = (short)Width;
            TreeBox.Height = (short)Height;
            TreeBox.PosisionInvalid = false;
        }

        public override void Update(UpdateState state)
        {
            if (DoubleClickTime > 0) DoubleClickTime--;
            if (m_doDrag)
            {
                var position = Parent.GetMousePosition(state.MouseState);
                if (m_doResize)
                {
                    Width = Math.Max(100, (int)(position.X - Position.X));
                    Height = Math.Max(26, (int)(position.Y - Position.Y));
                    UpdateHitbox();
                    CommentResized();
                }
                else
                {
                    float X = position.X - m_dragOffsetX, 
                        Y = position.Y - m_dragOffsetY;
                    bool shouldSnap = Master.Editor.SnapPrims;
                    if (shouldSnap)
                    {
                        var snapPos = SnapToNearbyPrims(state, new Vector2(X,Y), Parent.GetMousePosition(state.MouseState), out _);
                        X = snapPos.X;
                        Y = snapPos.Y;
                    }
                    this.X = X;
                    this.Y = Y;
                }
                state.SharedData["ExternalDraw"] = true;
            }
            else if (Master.HoverPrim == this)
            {
                if (Type == TREEBoxType.Label && state.MouseState.RightButton == ButtonState.Pressed)
                {
                    //create a goto for this label
                    Master.Editor.SetPlacement(TREEBoxType.Goto, this);
                }
            }
            base.Update(state);
            if (InvalidationParent?.Invalidated == true) UpdateNodePos(state);
        }

        /// <summary>
        /// Gets a position that snaps the primitive to the closest primitive to the mouse cursor.
        /// </summary>
        /// <param name="state">The current UpdateState</param>
        /// <param name="defaultPosition">The position to return if no primitive was snapped to</param>
        /// <param name="mousePosition">The position of the mouse relative to the primitives to snap to</param>
        /// <param name="snapped">Represents if a primitive was able to be snapped to.</param>
        /// <returns>The calculated position snapped to a primitive in the BHAVContainer </returns>
        public Vector2 SnapToNearbyPrims(UpdateState state, Vector2 defaultPosition, Vector2 mousePosition, out bool snapped)
        {
            snapped = false;
            if (Master == null) // if this primitive doesn't belong to a container it can't snap anywhere.
                return defaultPosition;            
            PrimitiveBox closestPrim = null;
            double hitboxMarginX = 150, hitboxMarginY = 90; // margins that apply only to the hitbox
            var mousePos = mousePosition;
            var potentials = new List<Tuple<PrimitiveBox, double>>(); // when multiple boxes are close to each other, pick the one closest to the mouse to snap to.
            foreach (var prim in Master.Primitives)
            {
                if (prim == this)
                    continue;
                Rectangle r = new Rectangle((int)(prim.X - hitboxMarginX),
                                            (int)(prim.Y - hitboxMarginY),
                                            (int)(prim.Width + hitboxMarginX * 2),
                                            (int)(prim.Height + hitboxMarginY * 2)); // create a hitbox around the prim to test if the mouse is inside it
                double a = prim.CenX - mousePos.X;
                double b = prim.CenY - mousePos.Y;
                var dist = Math.Sqrt(a * a + b * b); // distance from the prim to the mouse

                if (r.Contains(mousePos))                
                    potentials.Add(Tuple.Create(prim, dist));                
            }
            if (!potentials.Any())
                return defaultPosition;
            if (potentials.Count == 1)
                closestPrim = potentials.First().Item1;
            else
                closestPrim = potentials.Where(x => x.Item2 == potentials.Select(y => y.Item2).Min()).FirstOrDefault()?.Item1; // compare the distances from the mouse for each prim and return the closest
            if (closestPrim == null) // should never be the case
                return defaultPosition;
            bool above = mousePos.Y < closestPrim.CenY;
            double newX = 0, // new X and Y coords relative to the snapping target
                   newY = 0,
                   margin = 45; // margin between this prim and the one being snapped to
            var relMousePos = mousePos - closestPrim.Position;
            if (mousePos.X > closestPrim.X && mousePos.X < closestPrim.X + closestPrim.Width) // x-axis snap
            {
                if (closestPrim.Width < Width) // if this prim is bigger than the prim to snap to, just default to snapping to the middle
                {
                    newX = (closestPrim.Width / 2) - (Width / 2);
                    newY = (above) ? -margin - Height : margin + closestPrim.Height;
                }
                else
                {
                    if (relMousePos.X < closestPrim.Width / 3) // mouse in first third of closest prim (left)         
                    {
                        newX = 0;
                        newY = (above) ? -margin - Height : margin + closestPrim.Height;
                    }
                    else if (relMousePos.X < 2 * (closestPrim.Width / 3)) // mouse in second third (center)
                    {
                        newX = (closestPrim.Width / 2) - (Width / 2);
                        newY = (above) ? -margin - Height : margin + closestPrim.Height;
                    }
                    else if (relMousePos.X < closestPrim.Width) // (right)
                    {
                        newX = closestPrim.Width - Width;
                        newY = (above) ? -margin - Height : margin + closestPrim.Height;
                    }
                }
            }
            else if (mousePos.Y > closestPrim.Y && mousePos.Y < closestPrim.Y + closestPrim.Height) // y-axis snap
            {
                if (mousePos.X < closestPrim.CenX) // (left)
                    newX = -Width - margin;
                else // (right)
                    newX = closestPrim.Width + margin;
                newY = closestPrim.Height / 2 - (Height / 2);
            }
            else // ignore potentially accidental snaps
            {
                return defaultPosition;
            }
            snapped = true;
            return new Vector2((float)(closestPrim.X + newX), (float)(closestPrim.Y + newY));
        }

        public void UpdateNodePos(UpdateState state)
        {
            //we want to put nodes on the side closest to the destination. For this we use a vector from this node to the closest point on the destination.
            //to avoid crossover the side lists should be ordered by Y position.

            if (Nodes.Length == 0) return;

            var dirNodes = new List<PrimitiveNode>[4];  
            for (int i = 0; i < 4; i++) dirNodes[i] = new List<PrimitiveNode>();
            //0 = down, 1 = left, 2 = up, 3 = right

            for (int i = 0; i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                if (!node.Visible) continue;
                var centerPos = Position + new Vector2(Width / 2, Height / 2);
                var vec = (node.MouseDrag)?(GetMousePosition(state.MouseState)- new Vector2(Width / 2, Height / 2)) :(((node.Destination == null)?centerPos:node.Destination.NearestDestPt(centerPos)) - centerPos);

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
                    var dest = (vec.Y >= 0) ? 0 : 2;
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
                var bSize = (i % 2 == 0) ? Width : Height;
                var list = dirNodes[i];
                bSize /= Math.Max(1,list.Count);
                bStart += bSize/2;
                foreach (var node in list)
                {
                    node.Direction = i;
                    if (i % 2 == 0)
                        node.Position = new Vector2(bStart, (i == 0) ? Height+6 : -6);
                    else
                        node.Position = new Vector2((i == 3) ? Width+6 : -6, bStart);

                    bStart += bSize;
                }
            }
        }

        public Vector2 NearestDestPt(Vector2 pt)
        {
            return new Vector2(Math.Min(Math.Max(X, pt.X), X + Width), Math.Min(Math.Max(Y, pt.Y), Y + Height));
        }
    }
}
