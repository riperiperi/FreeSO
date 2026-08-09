using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class ValidationTests
    {
        private static JObject LoadExample()
        {
            return JObject.Parse(File.ReadAllText(TestPaths.Example("gossip-gnome.json")));
        }

        private static CompileResult Validate(JObject pack)
        {
            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());
            return PackCompilerApi.Validate(path);
        }

        [Fact]
        public void UnknownField_IsErrorWithPath()
        {
            var pack = LoadExample();
            var node = (JObject)pack["objects"][0]["trees"]["gossip_action"]["nodes"][0];
            node["sparkle"] = true;

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors,
                e => e.Contains("$.objects[0].trees.gossip_action.nodes[0].sparkle") && e.Contains("unknown field"));
        }

        [Fact]
        public void UnresolvedLabel_IsError()
        {
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][0]["then"] = "no_such_node";

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unresolved label \"no_such_node\""));
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void BareTrueFalse_IsAcceptedAsReturnLabel(string label)
        {
            // The most repeated authoring mistake by a wide margin — four times in one
            // measured run, despite the error naming the valid values. Accepted as an alias
            // rather than diagnosed better, because repeating an explained error is an
            // affordance problem.
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][0]["then"] = label;

            var result = Validate(pack);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
        }

        [Fact]
        public void NodeNamedTrue_StillWinsOverTheAlias()
        {
            // The alias is resolved after the node index, so a real node keeps its name.
            var pack = LoadExample();
            var nodes = (JArray)pack["objects"][0]["trees"]["gossip_action"]["nodes"];

            // Append a node genuinely named "true" rather than renaming an existing one,
            // which would break whatever already branches to it.
            var real = (JObject)nodes[nodes.Count - 1].DeepClone();
            real["id"] = "true";
            real["then"] = "return true";
            real["else"] = "error";
            nodes.Add(real);

            ((JObject)nodes[0])["then"] = "true";

            var result = Validate(pack);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
        }

        [Fact]
        public void UnresolvedAttributeName_IsError()
        {
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][6]["lhs"]["name"] = "no_such_attr";

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unresolved attribute \"no_such_attr\""));
        }

        [Fact]
        public void UnknownScope_IsError()
        {
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][5]["lhs"]["scope"] = "my_vibes";

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unknown scope \"my_vibes\""));
        }

        [Fact]
        public void TreeOver253Nodes_IsError()
        {
            var pack = LoadExample();
            var nodes = new JArray();
            for (int i = 0; i < 254; i++)
            {
                nodes.Add(new JObject
                {
                    ["id"] = "n" + i,
                    ["prim"] = "expression",
                    ["lhs"] = new JObject { ["scope"] = "literal", ["value"] = 1 },
                    ["op"] = "==",
                    ["rhs"] = new JObject { ["scope"] = "literal", ["value"] = 1 },
                    ["then"] = "return true",
                    ["else"] = "return false"
                });
            }
            pack["objects"][0]["trees"]["gossip_test"]["nodes"] = nodes;

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("max is 253"));
        }

        [Fact]
        public void GuidCollision_IsError()
        {
            var pack = LoadExample();
            var objects = (JArray)pack["objects"];
            var copy = (JObject)objects[0].DeepClone();
            copy["id"] = "second_gnome";
            copy["name"] = "Second Gnome"; // same guid on purpose
            objects.Add(copy);

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("GUID collision"));
        }

        [Fact]
        public void ErrorsPreventEmit()
        {
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][0]["then"] = "no_such_node";

            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());
            var outDir = Path.Combine(dir, "out");

            var result = PackCompilerApi.Build(path, outDir);
            Assert.False(result.Success);
            Assert.False(Directory.Exists(outDir) && Directory.EnumerateFiles(outDir).Any());
        }

        [Fact]
        public void ExpressionAgainstUnwritableScope_IsWarningNotError()
        {
            var pack = LoadExample();
            // gossip_test compares literals; flip its operator to an assignment
            pack["objects"][0]["trees"]["gossip_test"]["nodes"][0]["op"] = "=";

            var result = Validate(pack);
            Assert.True(result.Success);
            Assert.Contains(result.Diagnostics.Warnings, w => w.Contains("unwritable scope"));
        }

        [Fact]
        public void TestObjectType_UsesObjectIdField()
        {
            // regression: operand field was "id", colliding with the node-label "id"
            var pack = LoadExample();
            var nodes = (JArray)pack["objects"][0]["trees"]["gossip_test"]["nodes"];
            nodes[0]["then"] = "type_check";
            nodes.Add(new JObject
            {
                ["id"] = "type_check",
                ["prim"] = "test_object_type",
                ["guid"] = "0x6B4F0001",
                ["object_id"] = new JObject { ["scope"] = "stack_object_id" },
                ["then"] = "return true",
                ["else"] = "return false"
            });

            var result = Validate(pack);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
        }

        [Fact]
        public void UnknownPrimitive_IsError()
        {
            var pack = LoadExample();
            pack["objects"][0]["trees"]["gossip_action"]["nodes"][0]["prim"] = "teleport_home";

            var result = Validate(pack);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unknown primitive \"teleport_home\""));
        }
    }
}
