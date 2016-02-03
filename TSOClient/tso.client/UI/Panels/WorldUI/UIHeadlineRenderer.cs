using FSO.Common.Utils;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Panels.WorldUI
{
    public class UIHeadlineRenderer : VMHeadlineRenderer
    {
        private static IffFile Sprites;
        private static Texture2D WhitePx;
        private static int[] GroupOffsets =
        {
            0x000,
            0x064,
            0x190,
            0x0C8,
            0x12C,
            0x1F4,
            0x258,
            0x000, //algorithmic
            0x2BC,
            0x320
        };
        private static int[] ZoomToDiv =
        {
            0,
            4,
            2,
            1
        };

        private RenderTarget2D Texture;
        private SPR Sprite;
        private SPR BGSprite;
        private WorldZoom LastZoom;
        private Texture2D AlgTex;
        private int ZoomFrame;

        private bool DrawSkill
        {
            get
            {
                return LastZoom == WorldZoom.Near && Headline.Operand.Group == VMSetBalloonHeadlineOperandGroup.Progress && Headline.Index < 10;
            }
        }

        public UIHeadlineRenderer(VMRuntimeHeadline headline) : base(headline)
        {
            if (Sprites == null)
            {
                Sprites = new Files.Formats.IFF.IffFile(Content.Content.Get().GetPath("objectdata/globals/sprites.iff"));
                WhitePx = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.White);
            }

            if (Headline.Operand.Group != VMSetBalloonHeadlineOperandGroup.Algorithmic)
                Sprite = Sprites.Get<SPR>((ushort)(GroupOffsets[(int)Headline.Operand.Group] + Headline.Index));

            if (Headline.Operand.Type != 255 && Headline.Operand.Type != 3)
                BGSprite = Sprites.Get<SPR>((ushort)(GroupOffsets[(int)VMSetBalloonHeadlineOperandGroup.Balloon]+Headline.Operand.Type));

            LastZoom = WorldZoom.Near;
            RecalculateTarget();
        }

        public void RecalculateTarget()
        {
            ZoomFrame = 3 - (int)LastZoom;

            if (Texture != null) Texture.Dispose();

            if (DrawSkill)
            {
                Texture = new RenderTarget2D(GameFacade.GraphicsDevice, 160, 49);
                return;
            }

            if (Sprite != null)
            {
                SPRFrame bigFrame = (BGSprite != null) ? BGSprite.Frames[ZoomFrame] : Sprite.Frames[ZoomFrame];
                Texture = new RenderTarget2D(GameFacade.GraphicsDevice, bigFrame.Width, bigFrame.Height);
            }
            else if (Headline.Operand.Group == VMSetBalloonHeadlineOperandGroup.Algorithmic && LastZoom != WorldZoom.Far)
            {
                AlgTex = (Headline.IconTarget == null)?WhitePx:Headline.IconTarget.GetIcon(GameFacade.GraphicsDevice, (int)LastZoom - 1);
                Point bigFrame = (BGSprite != null) ? new Point(BGSprite.Frames[ZoomFrame].Width, BGSprite.Frames[ZoomFrame].Height) : new Point(AlgTex.Width, AlgTex.Height);
                Texture = new RenderTarget2D(GameFacade.GraphicsDevice, bigFrame.X, bigFrame.Y);
            }
            else AlgTex = null;
        }

        public override Texture2D DrawFrame(World world)
        {
 
            if (LastZoom != world.State.Zoom)
            {
                LastZoom = world.State.Zoom;
                RecalculateTarget();
            }
            var GD = GameFacade.GraphicsDevice;
            var batch = new SpriteBatch(GD);

            GD.SetRenderTarget(Texture);
            GD.Clear(Color.Transparent);
            batch.Begin();

            if (BGSprite != null) batch.Draw(BGSprite.Frames[ZoomFrame].GetTexture(GD), new Vector2(), Color.White);

            Texture2D main = null;
            Vector2 offset = new Vector2();
            if (Sprite != null)
            {
                var animFrame = (Headline.Anim / 15) % (Sprite.Frames.Count / 3);
                main = Sprite.Frames[ZoomFrame + animFrame * 3].GetTexture(GD);
                offset = new Vector2(0, 4);
            }
            else if (AlgTex != null)
            {
                main = AlgTex;
                offset = new Vector2(0, -6);
            }
            offset /= ZoomToDiv[(int)LastZoom];

            if (main != null) batch.Draw(main, new Vector2(Texture.Width / 2 - main.Width / 2, Texture.Height / 2 - main.Height / 2)+offset, Color.White);

            if (Headline.Operand.Crossed)
            {
                Texture2D Cross = Sprites.Get<SPR>(67).Frames[ZoomFrame].GetTexture(GD);
                batch.Draw(Cross, new Vector2(Texture.Width / 2 - Cross.Width / 2, Texture.Height / 2 - Cross.Height / 2), Color.White);
            }

            if (DrawSkill)
            {
                batch.Draw(WhitePx, new Rectangle(88, 4, 71, 41), new Color(92, 92, 92));
                var font = GameFacade.MainFont.GetNearest(14).Font;

                var avatar = (VMAvatar)Headline.Target;

                string skillString = "Skill: " + (avatar.GetPersonData((VMPersonDataVariable)
                    (avatar.GetPersonData(VMPersonDataVariable.SkillEfficiency)>>8))/100.0).ToString("F2");
                Vector2 fontOrigin = font.MeasureString(skillString) / 2;
                batch.DrawString(font, skillString, new Vector2(88 + 35, 15) - fontOrigin*0.60f, new Color(255, 249, 157), 0, new Vector2(), 0.60f, SpriteEffects.None, 0);

                skillString = "Speed: " + (avatar.GetPersonData(VMPersonDataVariable.SkillEfficiency)&0xFF) + "%";
                fontOrigin = font.MeasureString(skillString) / 2;
                batch.DrawString(font, skillString, new Vector2(88 + 35, 34) - fontOrigin * 0.60f, new Color(255, 249, 157), 0, new Vector2(), 0.60f, SpriteEffects.None, 0);
            }

            batch.End();
            GD.SetRenderTarget(null);

            return Texture;
        }

        public override void Dispose()
        {
            if (Texture != null) Texture.Dispose();
        }
    }

    public class UIHeadlineRendererProvider : VMHeadlineRendererProvider
    {
        public VMHeadlineRenderer Get(VMRuntimeHeadline headline)
        {
            return new UIHeadlineRenderer(headline);
        }
    }
}
