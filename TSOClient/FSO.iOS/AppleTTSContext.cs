using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AVFoundation;
using Foundation;
using FSO.Client.UI.Panels;
using UIKit;

namespace FSOiOS
{
    public class AppleTTSContext : ITTSContext
    {
        public static AppleTTSContext PlatformProvider()
        {
            return new AppleTTSContext();
        }

        public override void Dispose()
        {
            
        }

        public override void Speak(string text, bool gender, int pitch)
        {
            var speechSynthesizer = new AVSpeechSynthesizer();
            var voci = AVSpeechSynthesisVoice.GetSpeechVoices();

            var choices = voci.Where(x => x.Description.Contains(gender ? "female" : "male"));
            //prefer us
            AVSpeechSynthesisVoice voice;
            voice = choices.FirstOrDefault(x => x.Language.ToLowerInvariant().Contains("en-gb"));
            if (voice == null) voice = choices.FirstOrDefault(x => x.Language.ToLowerInvariant().Contains("en-gb"));
            if (voice == null && voci.Length > 0) voice = voci[0];
            if (voice == null) return;

            var speechUtterance = new AVSpeechUtterance(text)
            {
                Rate = AVSpeechUtterance.DefaultSpeechRate,
                Voice = voice,
                Volume = 0.5f,
                PitchMultiplier = pitch / 100f
            };

            speechSynthesizer.SpeakUtterance(speechUtterance);
            speechSynthesizer.DidFinishSpeechUtterance += (sender, e) =>
            {
                speechSynthesizer.Dispose();
            };
        }
    }
}