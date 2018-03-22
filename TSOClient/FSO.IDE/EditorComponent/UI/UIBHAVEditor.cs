using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.IDE.EditorComponent.Model;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.IDE.EditorComponent.Commands;
using Microsoft.Xna.Framework.Input;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.IDE.EditorComponent.DataView;
using FSO.Client;
using FSO.Common.Utils;

namespace FSO.IDE.EditorComponent.UI
{
    public class UIBHAVEditor : UIContainer
    {
        public BHAVContainer BHAVView;

        private List<BHAVCommand> Commands = new List<BHAVCommand>();
        private List<VMModifyDataCommand> ValueChangeCmds = new List<VMModifyDataCommand>();
        private Stack<BHAVCommand> UndoStack = new Stack<BHAVCommand>();
        private Stack<BHAVCommand> RedoStack = new Stack<BHAVCommand>();

        private PrimitiveBox Placement;

        public int UndoRedoDir;

        private UILabel PlacingName;
        private UILabel PlacingDesc;

        private int CutoutPhase;

        private int LastWidth;
        private int LastHeight;

        private bool MouseWasDown;
        private bool RightMouseWasDown;
        private bool RedrawNext;

        private bool DebugMode;
        private VMEntity DebugEntity;
        public VMStackFrame DebugFrame;

        public event BHAVEditor.DisableDebuggerDelegate DisableDebugger;

        //Debug only buttons;
        private UIButton DebugGo;
        private UIButton DebugStepOver;
        private UIButton DebugStepIn;
        private UIButton DebugStepOut;
        private UIButton DebugTrue;
        private UIButton DebugFalse;
        private UIButton DebugReset;
        private UILabel DebugLabel;

        private Dictionary<ushort, BHAVContainer> ContainerByID;

        public UIBHAVEditor(BHAV target, EditorScope scope, VMEntity debugEnt)
        {
            if (debugEnt != null)
            {
                DebugMode = true;
                DebugEntity = debugEnt;
            }

            ContainerByID = new Dictionary<ushort, BHAVContainer>();
            BHAVView = new BHAVContainer(target, scope);
            ContainerByID.Add(target.ChunkID, BHAVView);
            this.Add(BHAVView);

            GameThread.NextUpdate(x =>
            {
                var basePrim = BHAVView.RealPrim.FirstOrDefault();
                if (basePrim != null) BHAVView.Position = GetCentralLocation(basePrim);
            });

            PlacingName = new UILabel();
            PlacingName.Alignment = TextAlignment.Center;
            PlacingName.Size = new Vector2(1, 1);
            PlacingName.CaptionStyle = TextStyle.DefaultLabel.Clone();
            PlacingName.CaptionStyle.Font = FSO.Client.GameFacade.EdithFont;
            PlacingName.CaptionStyle.Size = 15;
            PlacingName.CaptionStyle.Color = new Color(0, 102, 26);

            PlacingName.Caption = "Placing Report Metric";

            PlacingDesc = new UILabel();
            PlacingDesc.Alignment = TextAlignment.Center;
            PlacingDesc.Size = new Vector2(1, 1);
            PlacingDesc.CaptionStyle = TextStyle.DefaultLabel.Clone();
            PlacingDesc.CaptionStyle.Font = FSO.Client.GameFacade.EdithFont;
            PlacingDesc.CaptionStyle.Size = 12;
            PlacingDesc.CaptionStyle.Color = new Color(0, 102, 26);

            PlacingDesc.Caption = "Press ESC to cancel.";

            this.Add(PlacingName);
            this.Add(PlacingDesc);

            if (DebugMode)
            {
                this.Add(new UITracerBar());

                var resource = EditorResource.Get().Indexed;
                DebugFrame = debugEnt.Thread.Stack.LastOrDefault();
                UpdateDebugPointer(DebugFrame);
                DebugGo = new UIButton();
                DebugGo.Texture = resource[0];
                DebugGo.Tooltip = "Go";
                DebugGo.Position = new Vector2(10, 5);
                Add(DebugGo);
                DebugGo.OnButtonClick += DebugButtonClick;

                DebugStepIn = new UIButton();
                DebugStepIn.Tooltip = "Step In";
                DebugStepIn.Texture = resource[1];
                DebugStepIn.Position = new Vector2(35, 5);
                Add(DebugStepIn);
                DebugStepIn.OnButtonClick += DebugButtonClick;

                DebugStepOver = new UIButton();
                DebugStepOver.Tooltip = "Step Over";
                DebugStepOver.Texture = resource[2];
                DebugStepOver.Position = new Vector2(60, 5);
                Add(DebugStepOver);
                DebugStepOver.OnButtonClick += DebugButtonClick;

                DebugStepOut = new UIButton();
                DebugStepOut.Tooltip = "Step Out";
                DebugStepOut.Texture = resource[3];
                DebugStepOut.Position = new Vector2(85, 5);
                Add(DebugStepOut);
                DebugStepOut.OnButtonClick += DebugButtonClick;

                DebugTrue = new UIButton();
                DebugTrue.Tooltip = "Return True";
                DebugTrue.Texture = resource[4];
                DebugTrue.Position = new Vector2(LastWidth - 80, 5);
                Add(DebugTrue);
                DebugTrue.OnButtonClick += DebugButtonClick;

                DebugFalse = new UIButton();
                DebugFalse.Tooltip = "Return False";
                DebugFalse.Texture = resource[5];
                DebugFalse.Position = new Vector2(LastWidth - 55, 5);
                Add(DebugFalse);
                DebugFalse.OnButtonClick += DebugButtonClick;

                DebugReset = new UIButton();
                DebugReset.Tooltip = "Reset Object";
                DebugReset.Texture = resource[6];
                DebugReset.Position = new Vector2(LastWidth-30, 5);
                Add(DebugReset);
                DebugReset.OnButtonClick += DebugButtonClick;

                DebugLabel = new UILabel();
                DebugLabel.CaptionStyle = TextStyle.DefaultLabel.Clone();
                DebugLabel.CaptionStyle.Font = FSO.Client.GameFacade.EdithFont;
                DebugLabel.CaptionStyle.Size = 12;
                DebugLabel.CaptionStyle.Color = Color.White;
                DebugLabel.Caption = "Breakpoint Hit.";
                DebugLabel.Position = new Vector2(115, 9);
                Add(DebugLabel);
            }
        }

        private void DebugButtonClick(UIElement button)
        {
            if (button == DebugGo)
                if (DebugEntity.Thread.ThreadBreak != VMThreadBreakMode.Pause)
                    DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.Immediate;
                else
                    DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.Active;
            else if (button == DebugStepIn)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.StepIn;
            else if (button == DebugStepOver)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.StepOver;
            else if (button == DebugStepOut)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.StepOut;
            else if (button == DebugTrue)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.ReturnTrue;
            else if (button == DebugFalse)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.ReturnFalse;
            else if (button == DebugReset)
                DebugEntity.Thread.ThreadBreak = VMThreadBreakMode.Reset;
            else return;

            Resume();
        }

        public void Resume()
        {
            DebugGo.Tooltip = "Pause";
            DebugGo.Texture = EditorResource.Get().Indexed[7];
            DebugStepIn.Disabled = true;
            DebugStepOut.Disabled = true;
            DebugStepOver.Disabled = true;
            DebugTrue.Disabled = true;
            DebugFalse.Disabled = true;
            DebugLabel.Caption = "Running...";
            RedrawNext = true;
            if (DisableDebugger != null) DisableDebugger();
            BHAVView.DebugPointer = null;
        }

        public void NewBreak(VMStackFrame frame)
        {
            DebugGo.Tooltip = "Go";
            DebugGo.Texture = EditorResource.Get().Indexed[0];
            DebugStepIn.Disabled = false;
            DebugStepOut.Disabled = false;
            DebugStepOver.Disabled = false;
            DebugTrue.Disabled = false;
            DebugFalse.Disabled = false;
            var breakStr = frame.Thread.ThreadBreakString ?? "Stopped.";
            bool isException = breakStr[0] == '!';
            if (isException)
            {
                DebugLabel.CaptionStyle.Color = new Color(255, 255, 155, 255);
                breakStr = breakStr.Substring(1);
            } else
            {
                DebugLabel.CaptionStyle.Color = Color.White;
            }
            DebugLabel.Caption = breakStr;
            RedrawNext = true;
            DebugFrame = frame;
            UpdateDebugPointer(DebugFrame);
        }

        public Vector2 GetCentralLocation(PrimitiveBox box)
        {
            return new Vector2(((UIExternalContainer)Parent).Width / 2 - (box.X + box.Width / 2), ((UIExternalContainer)Parent).Height / 2 - (box.Y + box.Height / 2));
        }

        public void UpdateDebugPointer(VMStackFrame frame)
        {
            if (frame != null && BHAVView.EditTarget.ChunkID == frame.Routine.ID)
            {
                BHAVView.DebugPointer = BHAVView.RealPrim[frame.InstructionPointer];

                GameThread.NextUpdate(x =>
                {
                    var location = GetCentralLocation(BHAVView.DebugPointer);

                    GameFacade.Screens.Tween.To(BHAVView, 0.5f, new Dictionary<string, float>() {
                    { "AnimScrollX", location.X },
                    { "AnimScrollY", location.Y },
                },
                    TweenQuad.EaseOut);
                });
            }
            else
            {
                BHAVView.DebugPointer = null;
            }
        }

        public void SwitchBHAV(BHAV target, EditorScope scope, VMStackFrame frame)
        {
            Remove(BHAVView);
            if (ContainerByID.ContainsKey(target.ChunkID))
            {
                BHAVView = ContainerByID[target.ChunkID];
                AddAt(0, BHAVView);
            } else
            {
                BHAVView = new BHAVContainer(target, scope);
                ContainerByID.Add(target.ChunkID, BHAVView);
                AddAt(0, BHAVView);
            }
            if (DebugMode)
            {
                DebugFrame = frame;
                UpdateDebugPointer(frame);
            }
        }

        public override void Update(UpdateState state)
        {
            lock (Commands)
            {
                if (Commands.Count > 0) RedoStack.Clear();
                foreach(var cmd in Commands)
                {
                    state.SharedData["ExternalDraw"] = true;
                    cmd.Execute(BHAVView.EditTarget, this);
                    UndoStack.Push(cmd);
                }
                if (Commands.Count > 0) BHAVView.Scope.Object.Resource.Recache();
                Commands.Clear();
            }

            lock (ValueChangeCmds)
            {
                foreach (var cmd in ValueChangeCmds)
                {
                    cmd.Execute();
                }
                Commands.Clear();
            }

            while (UndoRedoDir > 0)
            {
                if (UndoStack.Count > 0)
                {
                    var cmd = UndoStack.Pop();
                    state.SharedData["ExternalDraw"] = true;
                    cmd.Undo(BHAVView.EditTarget, this);
                    RedoStack.Push(cmd);
                }
                UndoRedoDir--;
            }

            while (UndoRedoDir < 0)
            {
                if (RedoStack.Count > 0)
                {
                    var cmd = RedoStack.Pop();
                    state.SharedData["ExternalDraw"] = true;
                    cmd.Execute(BHAVView.EditTarget, this);
                    UndoStack.Push(cmd);
                }
                UndoRedoDir++;
            }

            if (RedrawNext)
            {
                state.SharedData["ExternalDraw"] = true;
                RedrawNext = false;
            }

            if (Placement != null)
            {
                Placement.Position = GlobalPoint(new Vector2(state.MouseState.X, state.MouseState.Y)) - (new Vector2(Placement.Width, Placement.Height) / 2);
                Placement.Style = PGroupStyles.ByType[PrimitiveGroup.Placement];
                state.SharedData["ExternalDraw"] = true;
                Placement.Update(state);

                PlacingName.Position = new Vector2(LastWidth / 2, LastHeight - 66);
                PlacingDesc.Position = new Vector2(LastWidth / 2, LastHeight - 48);

                var mx = state.MouseState.Position.X;
                var my = state.MouseState.Position.Y;

                if (MouseWasDown && (state.MouseState.LeftButton == ButtonState.Released)
                    && mx > 0 && mx < LastWidth && my > 0 && my < LastHeight)
                {
                    QueueCommand(new AddPrimCommand(Placement));
                    Placement.Position -= BHAVView.Position;
                    ClearPlacement();
                }
            }
            CutoutPhase++;
            MouseWasDown = state.MouseState.LeftButton == ButtonState.Pressed;
            base.Update(state);

            if (BHAVView.HoverPrim != null && (!RightMouseWasDown) && 
                state.MouseState.RightButton == ButtonState.Pressed
                && BHAVView.HoverPrim.Type == PrimBoxType.Primitive)
            {
                QueueCommand(new ToggleBreakpointCommand(BHAVView.HoverPrim));
            }

            RightMouseWasDown = state.MouseState.RightButton == ButtonState.Pressed;
        }

        public void UpdateOperand(PrimitiveBox target)
        {
            var newOp = new byte[8];
            target.Descriptor.Operand.Write(newOp);

            QueueCommand(new OpModifyCommand(target, newOp));
        }

        public void QueueCommand(BHAVCommand cmd)
        {
            lock (Commands)
            {
                Commands.Add(cmd);
            }
        }

        internal void QueueValueChange(VMModifyDataCommand cmd)
        {
            lock (ValueChangeCmds)
            {
                ValueChangeCmds.Add(cmd);
            }
        }


        public void SetPlacement(ushort primType)
        {
            PlacingName.Visible = true;
            PlacingDesc.Visible = true;
            
            if (primType == 254 || primType == 255)
            {
                Placement = new PrimitiveBox((primType == 254) ? PrimBoxType.True : PrimBoxType.False, BHAVView);
                PlacingName.Caption = "Placing Return " + ((primType == 254) ? "True" : "False");
            }
            else
            {
                Placement = new PrimitiveBox(new BHAVInstruction
                {
                    TruePointer = 253,
                    FalsePointer = 253,
                    Opcode = primType,
                    Operand = new byte[8]
                }, 255, BHAVView);
                PlacingName.Caption = "Placing " + Placement.TitleText;
            }
            Placement.Parent = this;
        }
        public void ClearPlacement()
        {
            PlacingName.Visible = false;
            PlacingDesc.Visible = false;
            Placement = null;
            RedrawNext = true;
        }

        public override void Draw(UISpriteBatch batch)
        {
            var width = batch.GraphicsDevice.Viewport.Width;
            var height = batch.GraphicsDevice.Viewport.Height;
            BHAVView.Width = width;
            BHAVView.Height = height;

            base.Draw(batch);
            if (Placement != null)
            {
                Placement.ShadDraw(batch);
                Placement.Draw(batch);
            }
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(4, height), Color.Black * 0.2f);
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(4, 0), new Vector2(width, 4), Color.Black * 0.2f);

            if (DebugMode)
            {
                if (width != LastWidth)
                {
                    DebugTrue.Position = new Vector2(width - 80, 5);
                    DebugFalse.Position = new Vector2(width - 55, 5);
                    DebugReset.Position = new Vector2(width - 30, 5);
                    GameThread.NextUpdate(x => Invalidate());
                }
            }

            if (Placement != null)
            {
                DrawCutoutLines(CutoutPhase, 5, Color.Black * 0.2f, batch);
                DrawCutoutLines(CutoutPhase, 0, new Color(0, 102, 26), batch);
            }

            LastWidth = width;
            LastHeight = height;
        }

        public void DrawCutoutLines(int phase, int offset, Color color, UISpriteBatch batch)
        {
            var width = batch.GraphicsDevice.Viewport.Width;
            var height = batch.GraphicsDevice.Viewport.Height;
            var res = EditorResource.Get();
            int margin = 24;

            int boxWidth = width - margin * 2;
            int boxHeight = height - margin * 2;

            int i = phase%32;
            bool draw = ((phase/32)%2) == 1;
            i -= 32;
            while (i < boxWidth)
            {
                if (draw)
                {
                    DrawLine(res.WhiteTex,
                    new Vector2(Math.Max(margin, margin+i) + offset, margin + offset),
                    new Vector2(Math.Min(width - margin, margin + i + 32) + offset, margin + offset),
                    batch, 4, color);
                }

                i += 32; draw = !draw;
            }
            i -= boxWidth + 32;
            draw = !draw;

            while (i < boxHeight)
            {
                if (draw)
                {
                    DrawLine(res.WhiteTex,
                    new Vector2(offset + width - margin, Math.Max(margin, margin + i) + offset),
                    new Vector2(offset + width - margin, Math.Min(height - margin, margin + i + 32) + offset),
                    batch, 4, color);
                }

                i += 32; draw = !draw;
            }
            i -= boxHeight + 32;
            draw = !draw;

            while (i < boxWidth)
            {
                if (draw)
                {
                    DrawLine(res.WhiteTex,
                    new Vector2(width-Math.Max(margin, margin + i) + offset, (height-margin) + offset),
                    new Vector2(width-Math.Min(width - margin, margin + i + 32) + offset, (height - margin) + offset),
                    batch, 4, color);
                }

                i += 32; draw = !draw;
            }
            i -= boxWidth + 32;
            draw = !draw;

            while (i < boxHeight)
            {
                if (draw)
                {
                    DrawLine(res.WhiteTex,
                    new Vector2(offset + margin, height-Math.Max(margin, margin + i) + offset),
                    new Vector2(offset + margin, height-Math.Min(height - margin, margin + i + 32) + offset),
                    batch, 4, color);
                }

                i += 32; draw = !draw;
            }

        }

        private void DrawLine(Texture2D Fill, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth, Color tint) //draws a line from Start to End.
        {
            Start = LocalPoint(Start);
            End = LocalPoint(End);
            Start.Y += lineWidth / 2;
            End.Y += lineWidth / 2;
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            spriteBatch.Draw(Fill, new Rectangle((int)Start.X, (int)Start.Y - (int)(lineWidth / 2), (int)length, lineWidth), null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); //
        }

    }

    public class UITracerBar : UIElement
    {
        public override void Draw(UISpriteBatch batch)
        {
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(batch.GraphicsDevice.Viewport.Width, 30), new Color(12, 61, 112) * 0.80f);
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(0, 30), new Vector2(batch.GraphicsDevice.Viewport.Width, 4), new Color(12, 61, 112) * 0.30f);
        }
    }
}
