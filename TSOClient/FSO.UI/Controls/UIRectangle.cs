using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Controls
{
    public class UIRectangle : UIElement
    {
        private Color color = Color.White;
        private UIMouseEventRef _Mouse;

        public UIRectangle()
        {
            _Mouse = ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 50, 50), new UIMouseEvent(OnMouse));
        }

        private bool isDown = false;

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseOver)
            {
                if (isDown) { return; }
                color = Color.Red;
            }
            else if (type == UIMouseEventType.MouseOut)
            {
                if (isDown) { return; }
                color = Color.White;
            }
            else if (type == UIMouseEventType.MouseDown)
            {
                color = Color.Blue;
                isDown = true;
            }
            else if (type == UIMouseEventType.MouseUp)
            {
                isDown = false;
                color = Color.Green;
            }
        }

        public void SetSize(int width, int height)
        {
            _Mouse.Region = new Rectangle(0, 0, width, height);
        }

        //public override void Update(TSOClient.Code.UI.Model.UpdateState state)
        //{
        //    base.Update(state);

        //    /** Hit test **/
        //    color = Color.White;
        //    if (HitTestArea(state, new Microsoft.Xna.Framework.Rectangle(0, 0, 50, 50)))
        //    {
                
        //    }

        //    //color
        //}


        public override void Draw(UISpriteBatch batch)
        {
            var whiteRectangle = new Texture2D(batch.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { color });

            var pos = LocalRect(_Mouse.Region.X, _Mouse.Region.Y, _Mouse.Region.Width, _Mouse.Region.Height);
            batch.Draw(whiteRectangle, pos, Color.White);
        }
    }
}
