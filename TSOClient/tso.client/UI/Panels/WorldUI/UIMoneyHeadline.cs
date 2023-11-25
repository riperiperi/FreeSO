using FSO.SimAntics.Model;
using FSO.LotView;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.WorldUI
{
    public class UIMoneyHeadline : VMHeadlineRenderer
    {
        private RenderTarget2D MoneyTarget;
        private TextStyle Style;
        private string Text;
        private bool Inited;
        private VMRuntimeHeadline Runtime;

        public override bool IsMoney { get => true; }

        public UIMoneyHeadline(VMRuntimeHeadline headline) : base(headline)
        {
            Runtime = headline;
        }

        public override Texture2D DrawFrame(World world)
        {
            if (!Inited)
            {
                Style = TextStyle.DefaultLabel.Clone();
                var value = (int)(Runtime.Operand.Flags2 | (ushort)(Runtime.Operand.Duration << 16));
                Text = (value > 0) ? ("$" + value) : ("-$" + value);
                var measure = Style.MeasureString(Text);

                var GD = GameFacade.GraphicsDevice;
                MoneyTarget = new RenderTarget2D(GD, (int)measure.X + 10, (int)measure.Y + 3);

                GD.SetRenderTarget(MoneyTarget);
                GD.Clear(new Color(48, 69, 90));
                var batch = GameFacade.Screens.SpriteBatch;
                Style.VFont.Draw(GD, Text, new Vector2(5, 1), Style.Color, new Vector2(Style.Scale), null);

                /*
                batch.Begin();
                batch.DrawString(Style.SpriteFont, Text, new Vector2(5, 1), Style.Color);
                batch.End();
                */
                GD.SetRenderTarget(null);
                Inited = true;
            }
            return MoneyTarget;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
