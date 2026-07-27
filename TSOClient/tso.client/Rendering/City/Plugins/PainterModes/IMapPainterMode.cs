using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal interface IMapPainterMode
    {
        CityEditBase Command { get; }

        void TileHover(Vector2? tile);

        void TileMouseDown(Vector2 tile);

        void TileMouseUp(Vector2? tile);

        void Update(UpdateState state);

        void Draw(SpriteBatch sb);

        public static void BrushFunc(int width, Callback<int, int, float> callback)
        {
            var boxWidth = width * 2 + 1;
            for (int y = 0; y < boxWidth; y++)
            {
                for (int x = 0; x < boxWidth; x++)
                {
                    var dist = Math.Sqrt((x - width) * (x - width) + (y - width) * (y - width)) / (width + 0.5);
                    int targetX = x - width;
                    int targetY = y - width;

                    callback(targetX, targetY, (float)Math.Max(0, Math.Cos(dist * Math.PI / 2)));
                }
            }
        }
    }
}
