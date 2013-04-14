using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Rendering.City
{
    public interface ICityGeom
    {
        float CellWidth { get; set; }
        float CellHeight { get; set; }
        float CellYScale { get; set; }

        void Process(CityData city);
        void CreateBuffer(GraphicsDevice gd);
        void Draw(GraphicsDevice gd);
    }
}
