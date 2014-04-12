using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSO.Files.HIT
{
    public class HSM
    {
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
            ReadFile(File.Open(Filepath, FileMode.Open));
        }

        private void ReadFile(Stream stream)
        {
            var io = new StreamReader(stream);
            Constants = new Dictionary<string, int>();

            while (!io.EndOfStream)
            {
                string line = io.ReadLine();
                string[] Values = line.Split(' ');

                if (!Constants.ContainsKey(Values[0])) Constants.Add(Values[0], Convert.ToInt32(Values[1])); //the repeats are just labels for locations (usually called gotit)
            }

            io.Close();
        }
    }
}
