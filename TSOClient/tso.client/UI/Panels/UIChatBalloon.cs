using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Controls;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Common.Utils;
//using System.Speech.Synthesis;

namespace FSO.Client.UI.Panels
{
    public class UIChatBalloon : UIContainer, IDisposable
    {
        private Texture2D BPointerBottom;
        private Texture2D BPointerSide;
        private Texture2D BTiles;

        private static bool ProcessedBGFX = false;

        private TextRendererResult BodyTextLabels;
        private List<Vector2> BTOffsets;
        private TextStyle BodyTextStyle;
        private TextStyle ShadowStyle;
        private string BodyText;
        public int FadeTime;

        private UIChatPanel Owner;

        public Color Color;
        private Color BgColor = new Color(8,8,128); // default balloon color

        public string Name;
        public string Message;
        public float Alpha;
        public bool Gender;

        public Vector2 TargetLocation;

        public Rectangle DisplayRect;
        public Vector2 TargetPt;

        public Point DesiredRectPos;
        private bool Offscreen;
        public int ClosestDir = 3; //left, up, right, down, N/A

        public ITTSContext TTSContext;

        public UIChatBalloon(UIChatPanel owner)
        {
            Owner = owner;
            var gfx = Content.Content.Get().UIGraphics;
            //TODO: switch entire ui onto real content system

            BPointerBottom = GetTexture(0x1AF0856DDBAC);
            BPointerSide = GetTexture(0x1B00856DDBAC);
            BTiles = GetTexture(0x1B10856DDBAC);

            if (!ProcessedBGFX)
            {
                ProcessedBGFX = true;
                AlphaCopy(BPointerBottom);
                AlphaCopy(BPointerSide);
                AlphaCopy(BTiles);
            }

            BodyTextStyle = TextStyle.DefaultLabel.Clone();
            BodyTextStyle.Size = 12;
            BodyTextStyle.Color = new Color(240, 240, 48);

            ShadowStyle = BodyTextStyle.Clone();
            ShadowStyle.Color = Color.Black;

            /*
            if (!GameFacade.Linux && GlobalSettings.Default.EnableTTS)
            {
                TTSContext = (ITTSContext.Provider == null) ? null : ITTSContext.Provider();
            }*/
        }

        public void SetNameMessage(VMAvatar avatar)
        {
            Name = avatar.Name;
            Message = avatar.Message;
            Gender = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.Gender) > 0;
            TTSContext?.Speak(Message.Replace('_', ' '), Gender, ((VMTSOAvatarState)avatar.TSOState).ChatTTSPitch);

            if (((VMTSOAvatarState)avatar.TSOState).Permissions == VMTSOAvatarPermissions.Admin)            
                BgColor = new Color(180,0,0); // admin red color            
            else
                BgColor = new Color(8, 8, 128); // default blue color            
            Offscreen = false;
            if (Message == "") Name = "";
            TextChanged();
        }

        private string SanitizeBB(string input)
        {
            return BBCodeParser.SanitizeBB(input);
        }

        private void TextChanged()
        {
            BodyText = Message;
            if (GlobalSettings.Default.ChatOnlyEmoji > 0)
            {
                BodyText = GameFacade.Emojis.EmojiOnly(BodyText, GlobalSettings.Default.ChatOnlyEmoji);
            }
            BodyText = ((Offscreen && Message != "") ? "\\[" + Name + "] " : "") + GameFacade.Emojis.EmojiToBB(SanitizeBB(BodyText));

            var textW = Math.Max(130, Message.Length*2);
            BodyTextLabels = TextRenderer.ComputeText(BodyText, new TextRendererOptions
            {
                BBCode = true,
                Alignment = TextAlignment.Center,
                MaxWidth = textW,
                Position = new Microsoft.Xna.Framework.Vector2(18, 16),
                Scale = _Scale,
                TextStyle = BodyTextStyle,
                WordWrap = true,
            }, this);

            BTOffsets = new List<Vector2>();

            foreach (var cmd in BodyTextLabels.DrawingCommands)
            {
                if (cmd is INormalTextCmd) BTOffsets.Add(((INormalTextCmd)cmd).Position);
            }

            DisplayRect.Width = textW + 18 * 2;
            DisplayRect.Height = BodyTextLabels.BoundingBox.Height + 18 * 3;
        }

        private void AlphaCopy(Texture2D tex)
        {
            var data = new Color[tex.Width * tex.Height];
            tex.GetData<Color>(data);
            for (int i = 0; i < data.Length; i++)
            {
                data[i].A = data[i].R;
            }
            tex.SetData<Color>(data);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            UpdateDesiredPosition();

            DisplayRect.Location = DesiredRectPos;

            DeterminePointSide();
        }

        public void UpdateDesiredPosition()
        {
            DesiredRectPos = new Point((int)(TargetPt.X - DisplayRect.Width / 2), (int)(TargetPt.Y - (DisplayRect.Height + 20)));
            var dr = new Rectangle(DesiredRectPos, new Point(DisplayRect.Width, DisplayRect.Height)); 

            bool changed = false;
            foreach (var area in Owner.GetInvalid(this))
            {
                if (dr.Intersects(area))
                {
                    //move desired rectangle out of area
                    //first determine problem direction
                    var xDist = (area.X + area.Width / 2) - (dr.X + dr.Width / 2);
                    var yDist = (area.Y + area.Height / 2) - (dr.Y + dr.Height / 2);

                    if (Math.Abs(xDist) > Math.Abs(yDist))
                    {
                        if (xDist < 0) dr.X = area.Right;
                        else dr.X = area.Left-dr.Width;
                    } else
                    {
                        if (yDist < 0) dr.Y = area.Bottom;
                        else dr.Y = area.Top-dr.Height;
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                if (!Offscreen)
                {
                    Offscreen = true;
                    TextChanged();
                }
            }
            else
            {
                if (Offscreen)
                {
                    Offscreen = false;
                    TextChanged();
                }
            }

            DesiredRectPos = dr.Location;
        }

        public void DeterminePointSide()
        {
            float xDist = TargetPt.X - (DisplayRect.X + DisplayRect.Width / 2);
            float ax = (Math.Abs(xDist) - DisplayRect.Width / 2);

            float yDist = TargetPt.Y - (DisplayRect.Y + DisplayRect.Height / 2);
            float ay = (Math.Abs(yDist) - DisplayRect.Height / 2);

            if (ax < 30 && ay < 30)
            {
                ClosestDir = 4;
            }

            if (ax > ay)
            {
                //x pointer
                if (DisplayRect.Height < 80) ClosestDir = 4; //cannot fit horizontal arrow
                else if (xDist < 0) ClosestDir = 0; //left
                else ClosestDir = 2; //right
            }
            else
            {
                //y pointer
                if (yDist < 0) ClosestDir = 1; //up
                else ClosestDir = 3; //down
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (Alpha == 0) return;
            base.Draw(batch);
            Color bgCol = BgColor * Alpha;
            
            //draw corners
            DrawLocalTexture(batch, BTiles, new Rectangle(0, 0, 40, 40), new Vector2(DisplayRect.Left-20, DisplayRect.Top-20), Vector2.One, bgCol);
            DrawLocalTexture(batch, BTiles, new Rectangle(40, 0, 40, 40), new Vector2(DisplayRect.Right + 20, DisplayRect.Top - 20), new Vector2(-1,1), bgCol);
            DrawLocalTexture(batch, BTiles, new Rectangle(80, 0, 40, 40), new Vector2(DisplayRect.Right + 20, DisplayRect.Bottom + 20), new Vector2(-1, -1), bgCol);
            DrawLocalTexture(batch, BTiles, new Rectangle(120, 0, 40, 40), new Vector2(DisplayRect.Left - 20, DisplayRect.Bottom + 20), new Vector2(1, -1), bgCol);

            //draw edges
            //if the pointer is on this edge, it needs to be split into 3... Before point, point and after point. 

            var vertH = DisplayRect.Height - 40;
            var vertPt = (int)Math.Max(DisplayRect.Top + 20, Math.Min(DisplayRect.Bottom - 60, TargetPt.Y-20)) - (DisplayRect.Top + 20);

            var horizW = DisplayRect.Width - 40;
            var horizPt = (int)Math.Max(DisplayRect.Left + 20, Math.Min(DisplayRect.Right - 60, TargetPt.X-20)) - (DisplayRect.Left + 20);

            int ptSel = 0;

            //left
            if (ClosestDir == 0)
            {
                ptSel = Math.Max(0, Math.Min(3, (int)Math.Floor((DisplayRect.Left - TargetPt.X) / 40f)));
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Left - 20, DisplayRect.Top + 20), new Vector2(1, vertPt / 40f), bgCol);
                DrawLocalTexture(batch, BPointerSide, new Rectangle(0, ptSel * 40, 200, 40), new Vector2(DisplayRect.Left - 180, DisplayRect.Top + 20 + vertPt), Vector2.One, bgCol);
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Left - 20, DisplayRect.Top + 60 + vertPt), new Vector2(1, (vertH-(vertPt+40)) / 40f), bgCol);
            }
            else DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Left - 20, DisplayRect.Top + 20), new Vector2(1, (DisplayRect.Height-40)/40f), bgCol);

            //top
            if (ClosestDir == 1)
            {
                ptSel = Math.Max(0, Math.Min(3, (int)Math.Floor((DisplayRect.Top - TargetPt.Y) / 40f)));
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 80, 40, 40), new Vector2(DisplayRect.Left + 20, DisplayRect.Top - 20), new Vector2(horizPt / 40f, 1), bgCol);
                DrawLocalTexture(batch, BPointerBottom, new Rectangle(ptSel * 40, 0, 40, 200), new Vector2(DisplayRect.Left + 20 + horizPt, DisplayRect.Top + 20), new Vector2(1, -1), bgCol);
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 80, 40, 40), new Vector2(DisplayRect.Left + 60 + horizPt, DisplayRect.Top - 20), new Vector2(((horizW - (horizPt + 40)) / 40f), 1), bgCol);
            }
            else DrawLocalTexture(batch, BTiles, new Rectangle(0, 80, 40, 40), new Vector2(DisplayRect.Left + 20, DisplayRect.Top - 20), new Vector2((DisplayRect.Width - 40) / 40f, 1), bgCol);

            //right
            if (ClosestDir == 2)
            {
                ptSel = Math.Max(0, Math.Min(3, (int)Math.Floor((TargetPt.X - DisplayRect.Right) / 40f)));
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Right + 20, DisplayRect.Top + 20), new Vector2(-1, vertPt / 40f), bgCol);
                DrawLocalTexture(batch, BPointerSide, new Rectangle(0, ptSel * 40, 200, 40), new Vector2(DisplayRect.Right + 180, DisplayRect.Top + 20 + vertPt), new Vector2(-1, 1), bgCol);
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Right + 20, DisplayRect.Top + 60 + vertPt), new Vector2(-1, (vertH - (vertPt + 40)) / 40f), bgCol);
            }
            else DrawLocalTexture(batch, BTiles, new Rectangle(0, 40, 40, 40), new Vector2(DisplayRect.Right + 20, DisplayRect.Top + 20), new Vector2(-1, (DisplayRect.Height - 40) / 40f), bgCol);

            //bottom
            if (ClosestDir == 3)
            {
                ptSel = Math.Max(0, Math.Min(3, (int)Math.Floor((TargetPt.Y - DisplayRect.Bottom) / 40f)));
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 120, 40, 40), new Vector2(DisplayRect.Left + 20, DisplayRect.Bottom - 20), new Vector2(horizPt / 40f, 1), bgCol);
                DrawLocalTexture(batch, BPointerBottom, new Rectangle(ptSel * 40, 0, 40, 200), new Vector2(DisplayRect.Left + 20 + horizPt, DisplayRect.Bottom - 20), Vector2.One, bgCol);
                DrawLocalTexture(batch, BTiles, new Rectangle(0, 120, 40, 40), new Vector2(DisplayRect.Left + 60 + horizPt, DisplayRect.Bottom - 20), new Vector2(((horizW - (horizPt + 40)) / 40f), 1), bgCol);
            }
            else DrawLocalTexture(batch, BTiles, new Rectangle(0, 120, 40, 40), new Vector2(DisplayRect.Left + 20, DisplayRect.Bottom - 20), new Vector2((DisplayRect.Width - 40) / 40f, 1), bgCol);

            //draw middle
            DrawLocalTexture(batch, BTiles, new Rectangle(40, 120, 1, 1), new Vector2(DisplayRect.Left + 20, DisplayRect.Top + 20), new Vector2(DisplayRect.Width-40, DisplayRect.Height-40), bgCol);

            
            Vector2 offpos = new Vector2(DisplayRect.X + 1, DisplayRect.Y + 1)*Scale;
            int posi = 0;
            foreach (var cmd in BodyTextLabels.DrawingCommands)
            {
                if (cmd is INormalTextCmd)
                {
                    ((INormalTextCmd)cmd).Style = ShadowStyle;
                    ((INormalTextCmd)cmd).Position = BTOffsets[posi++] + offpos;
                }
                if (cmd is TextDrawCmd_Emoji) ((TextDrawCmd_Emoji)cmd).Shadow = true;
            }

            ShadowStyle.Color = Color.Black * Alpha;
            TextRenderer.DrawText(BodyTextLabels.DrawingCommands, this, batch);

            posi = 0;
            offpos = new Vector2(DisplayRect.X, DisplayRect.Y) * Scale;
            foreach (var cmd in BodyTextLabels.DrawingCommands)
            {
                if (cmd is INormalTextCmd) {
                    ((INormalTextCmd)cmd).Style = BodyTextStyle;
                    ((INormalTextCmd)cmd).Position = BTOffsets[posi++] + offpos;
                }
                if (cmd is TextDrawCmd_Emoji) ((TextDrawCmd_Emoji)cmd).Shadow = false;
            }
            BodyTextStyle.Color = this.Color * Alpha;
            TextRenderer.DrawText(BodyTextLabels.DrawingCommands, this, batch);

            this.Position = new Vector2();
        }

        public void Dispose()
        {
            TTSContext?.Dispose();
        }
    }



    public abstract class ITTSContext
    {
        public static Func<ITTSContext> Provider;
        public abstract void Dispose();
        public abstract void Speak(string text, bool gender, int pitch);
    }
}
