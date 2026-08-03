using CommandLine;

namespace FSO.Packager
{
    [Verb("package-remeshes", HelpText = "Package remeshes in the FSO.Remeshes format")]
    public class PackageRemeshesOptions
    {
        [Value(0, Required = true, MetaName = "Source Directory")]
        public required string SourceDirectory { get; set; }

        [Option('l', "legacy", Default = false, HelpText = "Generate legacy packages")]
        public bool Legacy { get; set; }

        [Option('g', "games", Default = "freeso,simitone", HelpText = "Specify games to generate packages for, comma separated")]
        public required string Games { get; set; }

        [Option('o', "out", Default = "dist/", HelpText = "Directory to output packages to")]
        public required string OutDirectory { get; set; }
    }

    [Verb("release-remeshes", HelpText = "Weites version information to remesh packages, and generates manifest json that can be used with the FreeSO updater")]
    public class ReleaseRemeshesOptions
    {
        [Value(0, Required = true, MetaName = "Source Directory")]
        public required string SourceDirectory { get; set; }

        [Option('g', "games", Default = "freeso,simitone", HelpText = "Specify games to generate packages for, comma separated")]
        public required string Games { get; set; }
    }

    [Verb("dummy", HelpText = "Verb that does nothing")]
    public class DummyOptions
    {
    }
}
