using FSO.Client.UI.Panels;
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
            Synth = new System.Speech.Synthesis.SpeechSynthesizer();
            Synth.SetOutputToDefaultAudioDevice();
        }

        public override void Dispose()
        {
            Synth.Dispose();
        }

        public override void Speak(string text, bool gender)
        {
            Synth.SelectVoiceByHints((gender) ? System.Speech.Synthesis.VoiceGender.Female : System.Speech.Synthesis.VoiceGender.Male);
            Synth.SpeakAsync(text);
        }
    }
}
