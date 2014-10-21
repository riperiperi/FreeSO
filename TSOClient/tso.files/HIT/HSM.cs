/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Rhys Simpson. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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

                var name = Values[0].ToLower();
                if (!Constants.ContainsKey(name)) Constants.Add(name, Convert.ToInt32(Values[1])); //the repeats are just labels for locations (usually called gotit)
            }

            io.Close();
        }
    }
}
