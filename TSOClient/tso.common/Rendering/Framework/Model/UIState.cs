using Microsoft.Xna.Framework;

namespace FSO.Common.Rendering.Framework.Model
{
    public class UIState
    {
        public int Width;
        public int Height;
        public UITooltipProperties TooltipProperties = new UITooltipProperties();
        public string Tooltip;

        public void SetTooltip(UpdateState state, string message, Color color)
        {
            TooltipProperties.Show = true;
            TooltipProperties.Color = color;
            state.UIState.TooltipProperties.Opacity = 1;
            state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X, state.MouseState.Y);
            state.UIState.Tooltip = message;
            state.UIState.TooltipProperties.UpdateDead = false;
        }

        public void SetTooltip(UpdateState state, string message)
        {
            SetTooltip(state, message, Color.Black);
        }
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
