using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace tso.content.model
{
    public class AudioReference
    {
        public uint ID { get; set; }
        public AudioType Type { get; set; }
        public string FilePath { get; set; }

        public string Name
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }
    }
}
