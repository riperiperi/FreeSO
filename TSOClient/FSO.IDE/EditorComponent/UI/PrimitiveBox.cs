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
        public BHAVContainer Master;

        public PrimBoxType Type;

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
        private UILabel Title;
        private string BodyText;
        private TextRendererResult BodyTextLabels;
        private TextStyle BodyTextStyle;

        private UIMouseEventRef HitTest;

        public PrimitiveBox FalseUI
        {
            get { return (Nodes.Length > 0) ? Nodes[0].Destination : null; }
            set { Nodes[0].Destination = value; }
        }
        public PrimitiveBox TrueUI
        {
            get { return (Nodes.Length > 1) ? Nodes[1].Destination : null; }
            set { Nodes[1].Destination = value; }
        }

        public PrimitiveBox(PrimBoxType mode, BHAVContainer master)
        {
            Type = mode;
            if (mode == PrimBoxType.True) InstPtr = 254;
            else InstPtr = 255;
            Master = master;
            Nodes = new PrimitiveNode[0];
            Width = 32;
            Height = 32;
            HitTest = ListenForMouse(new Rectangle(0, 0, Width, Height), new UIMouseEvent(MouseEvents));
        }

        public PrimitiveBox(BHAVInstruction inst, byte ptr, BHAVContainer master)
        {
            Type = PrimBoxType.Primitive;
            Instruction = inst;
            Descriptor = PrimitiveRegistry.GetDescriptor(inst.Opcode);
            Operand = (VMPrimitiveOperand)Activator.CreateInstance(Descriptor.OperandType);
            Operand.Read(inst.Operand);
            InstPtr = ptr;

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
            Title.CaptionStyle.Size = 14;

            BodyTextStyle = TextStyle.DefaultLabel.Clone();
            BodyTextStyle.Font = FSO.Client.GameFacade.EdithFont;
            BodyTextStyle.Size = 12;

            this.Add(Nodes[0]);
            this.Add(Nodes[1]);

            HitTest = ListenForMouse(new Rectangle(0, 0, Width, Height), new UIMouseEvent(MouseEvents));

            Master = master;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            Descriptor.Operand = Operand;
            Style = PGroupStyles.ByType[Descriptor.Group];

            var title = Descriptor.GetTitle(Master.Scope);
            var titleWidth = Title.CaptionStyle.MeasureString(title).X;
            Title.Caption = title;
            Title.CaptionStyle.Color = Style.Title;

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

            Width = Math.Max((int)titleWidth, BodyTextLabels.MaxWidth)+10;
            Height = BodyTextLabels.BoundingBox.Height+20;
            Title.Size = new Vector2(Width, 24);

            var shift = (Width - 300) / 2;
            foreach (var cmd in BodyTextLabels.DrawingCommands)
            {
                if (cmd is TextDrawCmd_Text)
                {
                    ((TextDrawCmd_Text)cmd).Position.X += shift;
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

            HitTest.Region.Width = Width;
            HitTest.Region.Height = Height;
        }

        public void ShadDraw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(5,5), new Vector2(Width, Height), ShadCol);
            foreach (var child in Nodes)
            {
                child.ShadDraw(batch);
            }
        }

        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();

            if (Type == PrimBoxType.Primitive)
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
                        ((TextDrawCmd_Text)cmd).Position.X += shift;
                    }
                }
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            var res = EditorResource.Get();

            if (Type == PrimBoxType.Primitive)
            {
                if (InstPtr == 0)
                {
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(-3, -3), new Vector2(Width + 6, Height + 6), new Color(0x96, 0xFF, 0x73));
                    DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(-2, -2), new Vector2(Width + 4, Height + 4), new Color(0x46, 0x8C, 0x00)); //start point green
                }

                DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(Width, Height), Master.Selected.Contains(this)?Color.Red:Color.White); //white outline
                DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1, 1), new Vector2(Width - 2, Height - 2), Style.Background); //background
                DrawTiledTexture(batch, res.DiagTile, new Rectangle(1, 1, Width - 2, Height - 2), Color.White * Style.DiagBrightness);
                DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(1, 1), new Vector2(Width - 2, 20), Color.White * 0.66f); //title bg

                Title.Draw(batch);
                if (BodyTextLabels != null) TextRenderer.DrawText(BodyTextLabels.DrawingCommands, this, batch);
            }
            else
            {
                DrawLocalTexture(batch, (Type == PrimBoxType.True)?res.TrueReturn:res.FalseReturn, new Vector2());
            }
        }


        private bool m_doDrag;
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
                    Master.Select(this);

                    if (DoubleClickTime > 0 && Type == PrimBoxType.Primitive && Descriptor is SubroutineDescriptor)
                    {
                        var subD = (SubroutineDescriptor)Descriptor;
                        FSO.Client.Debug.IDEHook.IDE.InjectIDEInto(FSO.Client.GameFacade.Screens.CurrentUIScreen, null, Master.Scope.GetBHAV(subD.PrimID), Master.Scope.Object);
                    }
                    DoubleClickTime = 25;
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

        public override void Update(UpdateState state)
        {
            if (DoubleClickTime > 0) DoubleClickTime--;
            if (m_doDrag)
            {
                var position = Parent.GetMousePosition(state.MouseState);
                this.X = position.X - m_dragOffsetX;
                this.Y = position.Y - m_dragOffsetY;
                state.SharedData["ExternalDraw"] = true;
            }
            UpdateNodePos();
            base.Update(state);
        }

        public void UpdateNodePos()
        {
            //we want to put nodes on the side closest to the destination. For this we use a vector from this node to the closest point on the destination.
            //to avoid crossover the side lists should be ordered by Y position.

            if (Type != PrimBoxType.Primitive) return;

            var dirNodes = new List<PrimitiveNode>[4];  
            for (int i = 0; i < 4; i++) dirNodes[i] = new List<PrimitiveNode>();
            //0 = down, 1 = left, 2 = up, 3 = right

            for (int i = 0; i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                if (!node.Visible) continue;
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

    public enum PrimBoxType
    {
        Primitive,
        True,
        False
    }
}
