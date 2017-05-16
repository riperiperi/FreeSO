using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public UIMoneyHeadline(VMRuntimeHeadline headline) : base(headline)
        {
            Style = TextStyle.DefaultLabel.Clone();
            var value = (int)(headline.Operand.Flags2 | (ushort)(headline.Operand.Duration << 16));
            Text = (value > 0)?("$" + value):("-$"+ value);
            var measure = Style.MeasureString(Text);

            var GD = GameFacade.GraphicsDevice;
            MoneyTarget = new RenderTarget2D(GD, (int)measure.X+10, (int)measure.Y+3);

            GD.SetRenderTarget(MoneyTarget);
            GD.Clear(new Color(48, 69, 90));
            var batch = GameFacade.Screens.SpriteBatch;
            batch.Begin();
            batch.DrawString(Style.SpriteFont, Text, new Vector2(5, 1), Style.Color);
            batch.End();
            GD.SetRenderTarget(null);
        }

        public override Texture2D DrawFrame(World world)
        {
            return MoneyTarget;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
