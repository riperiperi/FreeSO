using FSO.Common.Domain.Realestate;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterSpraypaint
    {
        private const float MinimumSpray = 128;
        private const float Divisor = 1f / (256f + MinimumSpray);
        private readonly byte[] Noise;

        public MapPainterSpraypaint()
        {
            Noise = new byte[512 * 512];
        }

        public MapPainterSpraypaint(bool shared)
        {
            Noise = shared ? CityMapUtils.GetRawNoise() : new byte[512 * 512];
        }

        public void NewSeed()
        {
            var random = Random.Shared.Next();
            CityMapUtils.GetSpraypaintNoise(Noise, (uint)random);
        }

        public float GetSpraypaint(int index, float strength)
        {
            var dat = Noise[index];

            return Math.Min(strength, (MinimumSpray + dat) * Divisor * strength);
        }

        public float GetRoughEdge(int index, float strength, float brushSize)
        {
            var dat = Noise[index];

            var middleDist = 0.5f - Math.Abs(0.5f - strength);

            // The closer to the middle, the more the random noise varies the strength.

            return strength - (dat / 255f) * middleDist * 0.7f * Math.Min(1f, 5f / brushSize);
        }
    }
}
