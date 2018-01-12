using FSO.LotView.Components;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Model
{
    public class WeatherController
    {
        //right now, this bases its state on the current sim half day. temporary for christmas event.

        public float WeatherIntensity;
        public Vector4 FogColor;
        public Blueprint Bp;
        public ParticleComponent Current;
        public List<ParticleComponent> Particles;
        public Vector4? TintColor;
        public Color OutsideWeatherTint;
        public float Darken;

        public float[] ModeToIntensity = new float[]
        {
            0f, 0.25f, 1f, //snow
            0f, 0.25f, 1f //rain
        };

        public float[] ModeToDarken = new float[]
        {
            0f, 0f, 0f, //snow
            0f, 0.25f, 1f, //rain
        };

        public WeatherController(Blueprint bp) {
            Bp = bp;
            Particles = bp.Particles;
        }

        public WeatherController(List<ParticleComponent> particles)
        {
            Particles = particles;
        }

        public float LastI = -1;
        public int LastHour = -1;
        public bool LastEnabled;

        public void Update()
        {
            var now = DateTime.UtcNow;
            var i = Math.Min(((now.Minute * 60) + (now.Second)) / 150f, 1f);
            //DECEMBER TEMP: snow replace
            //TODO: tie to tuning, or serverside weather system.
            //right now this is based on an rng advanced relative to the current UTC hour, with a fixed seed.
            //should also eventually introduce rain

            var ocolor = TintColor ?? Bp.OutsideColor.ToVector4();
            var color = ocolor - new Vector4(0.35f) * 1.5f + new Vector4(0.35f);
            color.W = 1;
            var wint = WeatherIntensity;
            FogColor = (color * new Color(0x80, 0xC0, 0xFF, 0xFF).ToVector4()) * (1 - wint * 0.75f) + ocolor * (wint * 0.75f);
            FogColor.W = (wint) * (15 * 75f) + (1 - wint) * (300f * 75f);
            var enabled = WorldConfig.Current.Weather;

            if (LastI == i && LastHour == now.Hour && (Current?.Time ?? 0) < 100 && enabled == LastEnabled) return;

            var curInt = GetWeatherIntensity(now);
            var lastInt = GetWeatherIntensity(now - new TimeSpan(1, 0, 0));
            LastI = i;
            LastHour = now.Hour;
            LastEnabled = enabled;

            WeatherIntensity = ModeToIntensity[curInt] * i + ModeToIntensity[lastInt] * (1 - i);
            Darken = ModeToDarken[curInt] * i + ModeToDarken[lastInt] * (1 - i);

            OutsideWeatherTint = Color.Lerp(Color.White, new Color(159, 164, 181), Darken);

            if (Bp != null)
            {
                Bp.OutsideWeatherTint = new Color(159, 164, 181);
                Bp.OutsideWeatherTintP = Darken;
            }

            if (WeatherIntensity > 0.01f && enabled)
            {
                bool isFaded = false;
                //is the new weather different enough? does the old one need to be refreshed?
                if (Current != null && (Current.Time > 100 || Math.Abs(Current.WeatherIntensity - WeatherIntensity) > 0.04f))
                {
                    Current.FadeProgress = 0;
                    isFaded = true;
                    Current = null;
                }
                if (Current == null)
                {
                    Current = new ParticleComponent(Bp, Particles);
                    Current.Mode = (ParticleType)(curInt/3);
                    Current.FadeProgress = isFaded ? (float?)-1 : null;
                    Current.WeatherIntensity = WeatherIntensity;
                    Particles.Add(Current);
                }
            }
            else
            {
                if (Current != null)
                {
                    Current.FadeProgress = 0;
                    Current = null;
                }
            }
        }

        private int GetWeatherIntensity(DateTime time)
        {
            var distance = time - new DateTime(2017, 12, 1);
            var halfDay = (int)distance.TotalHours;

            var rand = new Random(389457023);
            for (int i=0; i<halfDay; i++)
            {
                rand.Next();
            }
            return Math.Max(2, rand.Next(5)) + 1;
        }
        
    }
}
