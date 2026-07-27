using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FSO.Files.HIT
{
    public class HSM
    {
        private static Regex CamelCaseRegex = new Regex("([a-z])([A-Z])");

        /// <summary>
        /// HSM is a plaintext format that names various HIT constants including subroutine locations.
        /// </summary>
        /// 
        public Dictionary<string, int> Constants;
        
        /// <summary>
        /// Creates a new hsm file.
        /// </summary>
        /// <param name="Filedata">The data to create the hsm from.</param>
        public HSM(byte[] Filedata)
        {
            ReadFile(new MemoryStream(Filedata));
        }

        /// <summary>
        /// Creates a new hsm file.
        /// </summary>
        /// <param name="Filedata">The path to the data to create the hsm from.</param>
        public HSM(string Filepath)
        {
            ReadFile(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void ReadFile(Stream stream)
        {
            var io = new StreamReader(stream);
            Constants = new Dictionary<string, int>();

            while (!io.EndOfStream)
            {
                string line = io.ReadLine();
                string[] Values = line.Split(' ');

                var name = Values[0].ToLowerInvariant();
                var normalName = NormalizeCase(Values[0]).ToLowerInvariant();
                var value = Convert.ToInt32(Values[1]);
                Constants[name] = value; //the repeats are just labels for locations (usually called gotit)
                if (name != normalName)
                {
                    Constants[normalName] = value;
                }
            }

            io.Close();
        }

        private string NormalizeCase(string value)
        {
            var matches = CamelCaseRegex.Matches(value);

            int addedChars = 0;

            foreach (Match match in matches)
            {
                value = value.Substring(0, match.Index + addedChars + 1) + '_' + value.Substring(match.Index + addedChars + 1, value.Length - (match.Index + addedChars + 1));
                addedChars++;
            }

            return value;
        }
    }
}
