using FSO.Client.UI.Panels;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FSO.Windows
{
    public class MacTTSContext : ITTSContext
    {
        public static MacTTSContext PlatformProvider()
        {
            return new MacTTSContext();
        }

        public MacTTSContext()
        {
        }

        public override void Dispose()
        {
        }

        public override void Speak(string text, bool gender, int ipitch)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Remove double-quotes from text.
            text = text.Replace("\"", string.Empty);
            // Remove any 'say' command control sequences.
            text = Regex.Replace(text, @"\[\[.*?\]\]", string.Empty); 

            // Determine the voice to use based on the gender.
            // Samantha and Alex are available on most systems.
            // If not available, it falls back to the default voice.
            string voice = gender ? "Samantha" : "Alex";

            // Convert pitch from 0-100 scale to 0-127 scale used by 'pbas' command.
            // Note: negative values are clamped to 0 on macOS, it doesn't allow lower pitch than 0 (the default pitch).
            int pbas = (int)(ipitch * 1.27);

            // Construct the command to send to 'say'.
            string command = $"[[pbas {pbas}]] {text}";

            // Create a ProcessStartInfo to specify the 'say' command and arguments.
            var psi = new ProcessStartInfo("say", $"-v {voice} \"{command}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                // Run the 'say' command.
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Handle exception silently to not disrupt the game if anything happens.
                Console.WriteLine("Error running 'say' command: " + ex.Message);
            }
        }
    }
}