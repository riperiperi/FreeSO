using FSO.Client.UI.Panels;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Audio;
using System;

namespace FSO.Client.UI.Model
{
    public class UITTSContext : ITTSContext
    {
        private System.Speech.Synthesis.SpeechSynthesizer Synth;

        public static UITTSContext PlatformProvider()
        {
            return new UITTSContext();
        }

        public UITTSContext()
        {
        }

        public override void Dispose()
        {
        }

        public override void Speak(string text, bool gender, int ipitch)
        {
            var Synth = new System.Speech.Synthesis.SpeechSynthesizer();
            Synth.SelectVoiceByHints((gender) ? System.Speech.Synthesis.VoiceGender.Female : System.Speech.Synthesis.VoiceGender.Male);
            if (text == "") return;
            var voci = Synth.GetInstalledVoices();
            var stream = new System.IO.MemoryStream();
            var pitch = Math.Max(0.1f, ipitch/100f + 1f); //below 0.1 is just stupid, so just clamp there.
            if (pitch < 1f)
                Synth.Rate = 10-(int)(pitch*10);
            else
                Synth.Rate = (int)(10 / pitch) - 10;
            Synth.SetOutputToWaveStream(stream);
            Synth.SpeakAsync(text);

            EventHandler<System.Speech.Synthesis.SpeakCompletedEventArgs> OnComplete = null;
            OnComplete = (obj, evt) =>
            {
                GameThread.NextUpdate((u) =>
                {
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    var sfx = SoundEffect.FromStream(stream);
                    var inst = sfx.CreateInstance();
                    inst.Pitch = pitch - 1f;
                    inst.Play();

                    GameThreadInterval interval = null;
                    interval = GameThread.SetInterval(() =>
                    {
                        if (inst.State == SoundState.Stopped)
                        {
                            sfx.Dispose(); //just catch and dispose these when appropriate
                            interval.Clear();
                        }
                    }, 1000);
                    Synth.Dispose();
                });
                Synth.SpeakCompleted -= OnComplete;
            };

            Synth.SpeakCompleted += OnComplete;
        }
    }
}
