using System.Collections.Generic;
using System.Linq;

namespace MSDFData
{
    public class FontDescription
    {
        public FontDescription(string path, params char[] characters)
        {
            this.Path = path;            
            this.Characters = characters.ToList().AsReadOnly();
        }

        public string Path { get; }        
        public IReadOnlyList<char> Characters { get; }
    }
}
