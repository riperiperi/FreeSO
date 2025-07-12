using FSO.Common;
using System.Collections.Generic;

namespace FSO.Client.Model.Archive
{
    public class ArchiveManifest : IniConfig
    {
        public override string HeadingComment => "Archive manifest";

        public ArchiveManifest(string path) : base(path) { }

        private Dictionary<string, string> _DefaultValues = new Dictionary<string, string>()
        {
            { "Name", "Untitled" },
            { "Description", "" },
            { "Size", "0" },
            { "ZipLocation", ""},
            { "ZipHash", ""},
            { "LocalDir", ""},
        };

        public override Dictionary<string, string> DefaultValues
        {
            get { return _DefaultValues; }
            set { _DefaultValues = value; }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public string ZipLocation { get; set; }
        public string ZipHash { get; set; }
        public string LocalDir { get; set; }
    }
}
