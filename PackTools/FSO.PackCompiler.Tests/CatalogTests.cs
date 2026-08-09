using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class CatalogTests
    {
        [Fact]
        public void CategoryNames_MapToBuyModeIndices()
        {
            // s indices per tso.client/UI/Panels/UIBuyMode.cs InitCategoryMap()
            Assert.Equal(12, Names.Categories["seating"]);
            Assert.Equal(13, Names.Categories["surfaces"]);
            Assert.Equal(14, Names.Categories["appliances"]);
            Assert.Equal(15, Names.Categories["electronics"]);
            Assert.Equal(16, Names.Categories["skill"]);
            Assert.Equal(17, Names.Categories["decorative"]);
            Assert.Equal(18, Names.Categories["misc"]);
            Assert.Equal(19, Names.Categories["lighting"]);
            Assert.Equal(20, Names.Categories["pets"]);
        }

        [Fact]
        public void UnknownCategory_IsError()
        {
            var pack = JObject.Parse(File.ReadAllText(TestPaths.Example("gossip-gnome.json")));
            pack["objects"][0]["category"] = "plumbing"; // TS1 category, not in TSO Buy Mode

            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());

            var result = PackCompilerApi.Validate(path);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unknown category \"plumbing\""));
        }

        [Fact]
        public void Build_EmitsCatalogFragment()
        {
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(TestPaths.Example("gossip-gnome.json"), outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var doc = new XmlDocument();
            doc.Load(Path.Combine(outDir, "catalog-entries.xml"));
            Assert.Equal("Catalog", doc.DocumentElement.Name);

            var p = doc.GetElementsByTagName("P").Cast<XmlElement>().Single();
            Assert.Equal("6B4F0001", p.GetAttribute("g"));
            Assert.Equal("17", p.GetAttribute("s")); // decorative
            Assert.Equal("120", p.GetAttribute("p"));
            Assert.Equal("gnome, gossip", p.GetAttribute("t"));
            Assert.Equal("Gossip Gnome", p.GetAttribute("n"));
        }

        [Fact]
        public void Install_UpsertsCatalogIdempotently()
        {
            var gameDir = TestPaths.TempDir();
            var objectsDir = Path.Combine(gameDir, "Objects");
            Directory.CreateDirectory(objectsDir);

            // fake pre-existing catalog with an unrelated entry
            var catalogPath = Path.Combine(objectsDir, "catalog_downloads.xml");
            File.WriteAllText(catalogPath,
                "<Catalog>\n  <P g=\"DEADBEEF\" s=\"18\" p=\"55\" n=\"Unrelated Thing\" r=\"1\" />\n</Catalog>");

            // first install
            var result = PackCompilerApi.Install(TestPaths.Example("gossip-gnome.json"), gameDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
            Assert.True(File.Exists(Path.Combine(objectsDir, "gossip_gnome.iff")));

            // re-install with a changed price
            var pack = JObject.Parse(File.ReadAllText(TestPaths.Example("gossip-gnome.json")));
            pack["objects"][0]["price"] = 250;
            var packPath = Path.Combine(TestPaths.TempDir(), "pack.json");
            File.WriteAllText(packPath, pack.ToString());
            var result2 = PackCompilerApi.Install(packPath, gameDir);
            Assert.True(result2.Success, string.Join("\n", result2.Diagnostics.Errors));

            var doc = new XmlDocument();
            doc.Load(catalogPath);
            var entries = doc.GetElementsByTagName("P").Cast<XmlElement>().ToList();
            Assert.Equal(2, entries.Count); // upserted, not duplicated

            var gnome = entries.Single(p => p.GetAttribute("g") == "6B4F0001");
            Assert.Equal("250", gnome.GetAttribute("p")); // price updated in place
            Assert.Equal("17", gnome.GetAttribute("s"));
            Assert.Equal("Gossip Gnome", gnome.GetAttribute("n"));

            var other = entries.Single(p => p.GetAttribute("g") == "DEADBEEF");
            Assert.Equal("55", other.GetAttribute("p")); // untouched
            Assert.Equal("Unrelated Thing", other.GetAttribute("n"));
            Assert.Equal("1", other.GetAttribute("r")); // extra attributes preserved
        }

        [Fact]
        public void Install_CreatesCatalogWhenMissing()
        {
            var gameDir = TestPaths.TempDir();
            var result = PackCompilerApi.Install(TestPaths.Example("gossip-gnome.json"), gameDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var catalogPath = Path.Combine(gameDir, "Objects", "catalog_downloads.xml");
            Assert.True(File.Exists(catalogPath));
            var doc = new XmlDocument();
            doc.Load(catalogPath);
            Assert.Equal("Catalog", doc.DocumentElement.Name);
            var p = doc.GetElementsByTagName("P").Cast<XmlElement>().Single();
            Assert.Equal("6B4F0001", p.GetAttribute("g"));
        }

        [Fact]
        public void MissingCategory_DefaultsToMisc()
        {
            var pack = JObject.Parse(File.ReadAllText(TestPaths.Example("gossip-gnome.json")));
            ((JObject)pack["objects"][0]).Remove("category");
            var dir = TestPaths.TempDir();
            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, pack.ToString());

            var outDir = Path.Combine(dir, "out");
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var doc = new XmlDocument();
            doc.Load(Path.Combine(outDir, "catalog-entries.xml"));
            var p = doc.GetElementsByTagName("P").Cast<XmlElement>().Single();
            Assert.Equal("18", p.GetAttribute("s")); // misc
        }
    }
}
