using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class ImportedAppearanceTests
    {
        private static string WriteObj(string dir, string name, string objContent, string mtlContent = null)
        {
            var objPath = Path.Combine(dir, name + ".obj");
            File.WriteAllText(objPath, objContent);
            if (mtlContent != null)
            {
                var mtlName = name + ".mtl";
                File.WriteAllText(Path.Combine(dir, mtlName), mtlContent);
            }
            return objPath;
        }

        private static JObject PackForMesh(string meshRelPath, string id = "imported_box", string guid = "0x6B4F0F20")
        {
            return new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "import-test", ["name"] = "Import Test", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = id,
                        ["guid"] = guid,
                        ["name"] = "Imported Box",
                        ["price"] = 50,
                        ["category"] = "misc",
                        ["appearance"] = new JObject
                        {
                            ["imported"] = new JObject
                            {
                                ["mesh"] = meshRelPath,
                                ["height"] = 1.0,
                                ["provenance"] = new JObject
                                {
                                    ["source"] = "Test Kit",
                                    ["license"] = "CC0",
                                    ["retrieved"] = "2026-08-11",
                                    ["model"] = "box",
                                },
                            },
                        },
                        ["trees"] = new JObject
                        {
                            ["main_loop"] = new JObject
                            {
                                ["args"] = new JArray(),
                                ["locals"] = new JArray(),
                                ["nodes"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = "idle", ["prim"] = "idle_for_input", ["ticks_param"] = 0,
                                        ["allow_push"] = true, ["then"] = "idle", ["else"] = "idle",
                                    },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };
        }

        [Fact]
        public void ImportedMesh_ProducesDgrpAndSpr2ChunksInline()
        {
            var dir = TestPaths.TempDir();
            WriteObj(dir, "box",
                "mtllib box.mtl\n" +
                "v -0.5 0  -0.5\nv  0.5 0  -0.5\nv  0.5 0   0.5\nv -0.5 0   0.5\n" +
                "v -0.5 1  -0.5\nv  0.5 1  -0.5\nv  0.5 1   0.5\nv -0.5 1   0.5\n" +
                "usemtl wood\n" +
                "f 1 2 3 4\nf 5 6 7 8\nf 1 2 6 5\nf 2 3 7 6\nf 3 4 8 7\nf 4 1 5 8\n",
                "newmtl wood\nKd 0.5 0.3 0.1\n");

            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, PackForMesh("box.obj").ToString());
            var outDir = TestPaths.TempDir();

            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "imported_box.iff"));
            Assert.NotEmpty(iff.List<DGRP>());
            Assert.NotEmpty(iff.List<SPR2>());
            Assert.NotEmpty(iff.List<PALT>());
            var objd = iff.List<OBJD>().Single();
            Assert.True(objd.BaseGraphicID > 0);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance imported"));
        }

        [Fact]
        public void MissingMesh_IsError()
        {
            var dir = TestPaths.TempDir();
            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, PackForMesh("missing.obj", "missing_mesh", "0x6B4F0F21").ToString());
            var result = PackCompilerApi.Build(packPath, TestPaths.TempDir());
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("failed to import mesh"));
        }

        [Fact]
        public void Imported_SurvivesCompileDecompileCompile()
        {
            var dir = TestPaths.TempDir();
            WriteObj(dir, "crate",
                "v 0 0 0\nv 1 0 0\nv 1 0 1\nv 0 0 1\nv 0 1 0\nv 1 1 0\nv 1 1 1\nv 0 1 1\n" +
                "f 1 2 3 4\nf 5 6 7 8\nf 1 2 6 5\nf 2 3 7 6\nf 3 4 8 7\nf 4 1 5 8\n");

            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, PackForMesh("crate.obj", "imported_crate", "0x6B4F0F22").ToString());

            var outDir1 = Path.Combine(dir, "out1");
            var build1 = PackCompilerApi.Build(packPath, outDir1);
            Assert.True(build1.Success, string.Join("\n", build1.Diagnostics.Errors));

            var decompiledPath = Path.Combine(dir, "decompiled.json");
            var decompile = PackCompilerApi.Decompile(Path.Combine(outDir1, "imported_crate.iff"), decompiledPath);
            Assert.True(decompile.Success, string.Join("\n", decompile.Diagnostics.Errors));
            Assert.DoesNotContain(decompile.Diagnostics.Warnings, w => w.Contains("placeholder"));

            var appearance = (JObject)JObject.Parse(File.ReadAllText(decompiledPath))["objects"][0]["appearance"];
            Assert.NotNull(appearance["imported"]);
            Assert.Equal("crate.obj", (string)appearance["imported"]["mesh"]);

            var outDir2 = Path.Combine(dir, "out2");
            var build2 = PackCompilerApi.Build(decompiledPath, outDir2);
            Assert.True(build2.Success, string.Join("\n", build2.Diagnostics.Errors));
        }

        [Fact]
        public void ImportBatch_SiblingDirectory_EmitsRelativeMeshPath()
        {
            var root = TestPaths.TempDir();
            var assets = Path.Combine(root, "assets");
            var examples = Path.Combine(root, "examples");
            Directory.CreateDirectory(assets);
            Directory.CreateDirectory(examples);
            File.WriteAllText(Path.Combine(assets, "chair.obj"),
                "v 0 0 0\nv 1 0 0\nv 1 0 1\nv 0 0 1\nv 0 1 0\nv 1 1 0\nv 1 1 1\nv 0 1 1\n" +
                "f 1 2 3 4\nf 5 6 7 8\nf 1 2 6 5\nf 2 3 7 6\nf 3 4 8 7\nf 4 1 5 8\n");
            var csv = Path.Combine(assets, "manifest.csv");
            File.WriteAllText(csv, "chair.obj,Chair,seating,1.5,false,chair\n");
            var packPath = Path.Combine(examples, "pack.json");

            ImportBatchGenerator.Generate(csv, packPath, "batch-test", "Batch Test");

            var mesh = (string)JObject.Parse(File.ReadAllText(packPath))["objects"][0]["appearance"]["imported"]["mesh"];
            Assert.False(Path.IsPathRooted(mesh), "mesh path must be pack-relative, got: " + mesh);
            Assert.Equal("../assets/chair.obj", mesh);

            var build = PackCompilerApi.Build(packPath, TestPaths.TempDir());
            Assert.True(build.Success, string.Join("\n", build.Diagnostics.Errors));
        }

        [Fact]
        public void ExampleImportedPacks_MeshPathsAreRelative()
        {
            foreach (var name in new[] { "plumbing-pilot.json", "kenney-tier1.json" })
            {
                var pack = JObject.Parse(File.ReadAllText(TestPaths.Example(name)));
                foreach (var obj in (JArray)pack["objects"])
                {
                    var mesh = (string)obj["appearance"]?["imported"]?["mesh"];
                    Assert.False(string.IsNullOrEmpty(mesh), name + " object missing mesh");
                    Assert.False(Path.IsPathRooted(mesh), name + " #" + obj["id"] + " has rooted mesh path: " + mesh);
                }
            }
        }

        [Fact]
        public void ImportedAndGenerated_IsError()
        {
            var appearance = new JObject
            {
                ["clone_from_guid"] = "0xC14849AC",
                ["imported"] = new JObject { ["mesh"] = "x.obj", ["height"] = 1.0 },
            };
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "x", ["name"] = "X", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "x", ["guid"] = "0x6B4F0F23", ["name"] = "X", ["price"] = 1,
                        ["appearance"] = appearance,
                        ["trees"] = new JObject
                        {
                            ["main_loop"] = new JObject
                            {
                                ["args"] = new JArray(), ["locals"] = new JArray(),
                                ["nodes"] = new JArray
                                {
                                    new JObject { ["id"] = "idle", ["prim"] = "idle_for_input", ["ticks_param"] = 0, ["allow_push"] = true, ["then"] = "idle", ["else"] = "idle" },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };
            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());
            var result = PackCompilerApi.Validate(path);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("mutually exclusive"));
        }
    }
}
