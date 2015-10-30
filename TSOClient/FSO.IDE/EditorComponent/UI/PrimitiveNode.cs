using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.IDE.EditorComponent.Commands;

namespace FSO.IDE.EditorComponent.UI
{
    public class PrimitiveNode : UIContainer
    {
        private static Dictionary<NodeType, Color> NodeColors = new Dictionary<NodeType, Color> {
            { NodeType.True, new Color(0x00,0xB2,0x2D) },
            { NodeType.False, new Color(0xB2,0x2D,0x00) },
            { NodeType.Done, new Color(0x00,0x59,0xB2) },
        };

        private static Vector2[] IconPos =
        {
            new Vector2(0,2),
            new Vector2(-2,0),
            new Vector2(0,-2),
            new Vector2(2,0),
        };

        private static Rectangle[] SmallShad =
        {
            new Rectangle(-12, -6, 24, 2),
            new Rectangle(4, -12, 2, 24),
            new Rectangle(-12, 4, 24, 2),
            new Rectangle(-6, -12, 2, 24),
        };

        private static Color ShadCol = new Color(0xAF, 0xAF, 0xA3);

        public PrimitiveBox Destination;
        public NodeType Type;
        public int Direction;

        private UIMouseEventRef HitTest;

        public bool MouseDrag;
        public Vector2 DragVec;

        private Vector2 ArrowVec;

        public PrimitiveNode()
        {
            HitTest = ListenForMouse(new Rectangle(-16, -16, 32, 32), new UIMouseEvent(MouseEvents));
        }

        private void MouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    MouseDrag = true;
                    break;
            }
        }

        public override void Update(UpdateState state)
        {
            if (MouseDrag)
            {
                DragVec = this.GetMousePosition(state.MouseState);
                state.SharedData["ExternalDraw"] = true;

                if (state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
                {
                    MouseDrag = false;

                    var box = (PrimitiveBox)Parent;
                    box.Master.Editor.QueueCommand(new ChangePointerCommand(box, box.Master.HoverPrim, (Type != NodeType.False)));
                }
            }
            base.Update(state);
        }

        public void ShadDraw(UISpriteBatch batch)
        {
            if (!Visible) return;
            if (Destination != null && Destination.Dead) Destination = null;

            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.NodeOutline, null, new Vector2(res.NodeOutline.Width / -2 + 5, res.NodeOutline.Height / -2 + 5), new Vector2(1f, 1f), ShadCol);

            if (!MouseDrag && Destination == null) return;

            var contextPos = Parent.Position + Position;
            ArrowVec = (MouseDrag)?DragVec:(Destination.NearestDestPt(contextPos) - contextPos);

            var dir = new Vector2(ArrowVec.X, ArrowVec.Y);
            dir.Normalize();

            DrawLine(res.WhiteTex, dir * 10 + new Vector2(5,5), (ArrowVec - dir * 5)+new Vector2(5,5), batch, 6, ShadCol);
            var arrowDir = (float)Math.Atan2(-dir.X, dir.Y);
            var arrowPos = LocalPoint((ArrowVec) + new Vector2(5,5));
            batch.Draw(res.ArrowHeadOutline, arrowPos, null, ShadCol, arrowDir, new Vector2(9, 19), _Scale, SpriteEffects.None, 0);

        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            if (!Visible) return;
            Vector2 dir = new Vector2();
            var res = EditorResource.Get();

            if (MouseDrag || Destination != null)
            {
                //draw Line bg
                dir = new Vector2(ArrowVec.X, ArrowVec.Y);
                dir.Normalize();
                DrawLine(res.WhiteTex, dir * 10, ArrowVec - dir * 5, batch, 6, Color.White);
            }

            //draw Node
            DrawLocalTexture(batch, res.NodeOutline, new Vector2(res.NodeOutline.Width / -2, res.NodeOutline.Height / -2));
            DrawLocalTexture(batch, res.Node, null, new Vector2(res.Node.Width / -2, res.Node.Height / -2), new Vector2(1f, 1f), NodeColors[Type]);

            if (MouseDrag || Destination != null)
            {
                //draw Arrow
                var arrowDir = (float)Math.Atan2(-dir.X, dir.Y);
                var arrowPos = LocalPoint(ArrowVec);
                batch.Draw(res.ArrowHeadOutline, arrowPos, null, Color.White, arrowDir, new Vector2(9, 19), _Scale, SpriteEffects.None, 0);
                batch.Draw(res.ArrowHead, arrowPos, null, NodeColors[Type], arrowDir, new Vector2(9, 19), _Scale, SpriteEffects.None, 0);

                //draw Line
                DrawLine(res.WhiteTex, dir * 10, ArrowVec - dir * 5, batch, 4, NodeColors[Type]);
            }

            Texture2D icon;
            switch (Type)
            {
                case NodeType.False:
                    icon = res.FalseNode;
                    break;
                case NodeType.True:
                    icon = res.TrueNode;
                    break;
                default:
                    icon = res.DoneNode;
                    break;
            }
            DrawLocalTexture(batch, icon, IconPos[Direction]-new Vector2(icon.Width/2, icon.Height/2));
            var shadRect = SmallShad[Direction];
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(shadRect.X, shadRect.Y), new Vector2(shadRect.Width, shadRect.Height), Color.Black*0.15f);
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

    public enum NodeType
    {
        True,
        False,
        Done
    }
}
