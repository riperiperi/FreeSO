﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Rendering.Framework.Model
{
    public class UIState
    {
        public int Width;
        public int Height;
        public UITooltipProperties TooltipProperties = new UITooltipProperties();
        public string Tooltip;
    }

    public class UITooltipProperties
    {
        public float Opacity;
        public Vector2 Position;
        public bool Show;
        public Color Color = Color.Black;
        public bool UpdateDead;
    }
}
