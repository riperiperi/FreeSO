using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace MSDFData
{
    public class Metrics
    {
        [ContentSerializer] private readonly float AdvanceBackend;
        [ContentSerializer] private readonly float ScaleBackend;
        [ContentSerializer] private readonly Vector2 TranslationBackend;

        public Metrics()
        {
            
        }

        public Metrics(float advance, float scale, Vector2 translation)
        {
            this.AdvanceBackend = advance;
            this.ScaleBackend = scale;
            this.TranslationBackend = translation;
        }

        public float Advance => this.AdvanceBackend;
        public float Scale => this.ScaleBackend;
        public Vector2 Translation => this.TranslationBackend;
    }
}
