using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dummy
{
    class Program
    {
        /// <summary>
        /// Just a place to do tests without loading the whole client
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var files2 = Directory.GetFiles("C:\\Windows\\Fonts");
            foreach (var file in files2)
            {
                System.Diagnostics.Debug.WriteLine(file);
            }

            if (true) { return; }

            /** Work out char ranges used in the UI **/
            var englishDir = @"C:\Program Files\Maxis\The Sims Online\TSOClient\gamedata\uitext\english.dir";
            var files = Directory.GetFiles(englishDir);
            var charsUsed = new List<int>();

            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                for (var i = 0; i < content.Length; i++)
                {
                    var character = (int)content[i];
                    if (!charsUsed.Contains(character))
                    {
                        charsUsed.Add(character);
                    }
                }
            }

            charsUsed.Sort();

            System.Diagnostics.Debug.WriteLine(String.Join(", ", charsUsed.Select(x => x.ToString()).ToArray()));


            /*
            var service = new TSOServiceClient.TSOServiceClient();
            var session = service.Authenticate(new TSOServiceClient.Model.AuthRequest {
                Username = "dazlee4",
                Password = "password"
            });

            var x = true;*/
        }
    }
}
