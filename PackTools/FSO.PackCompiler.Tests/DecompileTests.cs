using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class DecompileTests
    {
        /// <summary>
        /// The round-trip proof: compile → decompile → recompile must reproduce every
        /// BHAV instruction byte for byte, and TTAB/STR content must survive.
        /// </summary>
        [Fact]
        public void CompileDecompileRecompile_IsByteIdentical()
        {
            var dirA = TestPaths.TempDir();
            var first = PackCompilerApi.Build(TestPaths.Example("gossip-gnome.json"), dirA);
            Assert.True(first.Success, string.Join("\n", first.Diagnostics.Errors));

            var jsonPath = Path.Combine(TestPaths.TempDir(), "decompiled.json");
            var dec = PackCompilerApi.Decompile(Path.Combine(dirA, "gossip_gnome.iff"), jsonPath);
            Assert.True(dec.Success, string.Join("\n", dec.Diagnostics.Errors));
            Assert.Contains(dec.Diagnostics.Warnings, w => w.Contains("placeholder appearance"));

            var dirB = TestPaths.TempDir();
            var second = PackCompilerApi.Build(jsonPath, dirB);
            Assert.True(second.Success, string.Join("\n", second.Diagnostics.Errors));

            var a = new IffFile(Path.Combine(dirA, "gossip_gnome.iff"));
            var b = new IffFile(Path.Combine(dirB, "gossip_gnome.iff"));

            for (ushort id = 4096; id <= 4099; id++)
            {
                var ba = a.Get<BHAV>(id);
                var bb = b.Get<BHAV>(id);
                Assert.NotNull(bb);
                Assert.Equal(ba.Args, bb.Args);
                Assert.Equal(ba.Locals, bb.Locals);
                Assert.Equal(ba.Instructions.Length, bb.Instructions.Length);
                for (int i = 0; i < ba.Instructions.Length; i++)
                {
                    Assert.Equal(ba.Instructions[i].Opcode, bb.Instructions[i].Opcode);
                    Assert.Equal(ba.Instructions[i].TruePointer, bb.Instructions[i].TruePointer);
                    Assert.Equal(ba.Instructions[i].FalsePointer, bb.Instructions[i].FalsePointer);
                    Assert.Equal(ba.Instructions[i].Operand, bb.Instructions[i].Operand);
                }
            }

            var ta = a.Get<TTAB>(128).Interactions.Single();
            var tb = b.Get<TTAB>(128).Interactions.Single();
            Assert.Equal(ta.ActionFunction, tb.ActionFunction);
            Assert.Equal(ta.TestFunction, tb.TestFunction);
            Assert.Equal(ta.TTAIndex, tb.TTAIndex);
            Assert.Equal(ta.MotiveEntries[14].EffectRangeDelta, tb.MotiveEntries[14].EffectRangeDelta);
            Assert.Equal(ta.AllowVisitors, tb.AllowVisitors);
            Assert.Equal(ta.AllowObjectOwner, tb.AllowObjectOwner);
            Assert.Equal(ta.AllowRoommates, tb.AllowRoommates);
            Assert.Equal(ta.AllowGhosts, tb.AllowGhosts);

            Assert.Equal("Gossip", b.Get<TTAs>(128).GetString(0));
            Assert.Equal(a.Get<STR>(301).GetString(0), b.Get<STR>(301).GetString(0));
            Assert.Equal(a.Get<STR>(301).GetString(1), b.Get<STR>(301).GetString(1));
            Assert.Equal(a.Get<CTSS>(2000).GetString(0), b.Get<CTSS>(2000).GetString(0));

            var oa = a.List<OBJD>().Single();
            var ob = b.List<OBJD>().Single();
            Assert.Equal(oa.GUID, ob.GUID);
            Assert.Equal(oa.Price, ob.Price);
            Assert.Equal(oa.NumAttributes, ob.NumAttributes);
            Assert.Equal(oa.BHAV_MainID, ob.BHAV_MainID);
            Assert.Equal(oa.BHAV_Init, ob.BHAV_Init);
        }

        [Fact]
        public void Decompile_UnsupportedOpcode_FailsLoud()
        {
            var iff = new IffFile();
            var objd = new OBJD
            {
                ChunkID = 1,
                ChunkLabel = "Grabby",
                ChunkProcessed = true,
                ChunkType = "OBJD",
                ChunkParent = iff,
                ObjectType = OBJDType.Normal,
                GUID = 0x12345678,
                SubIndex = -1,
            };
            iff.AddChunk(objd);
            var bhav = new BHAV
            {
                ChunkID = 4096,
                ChunkLabel = "grab_tree",
                ChunkProcessed = true,
                ChunkType = "BHAV",
                ChunkParent = iff,
                Instructions = new[]
                {
                    // grab (0x04) is not a schema v0.1 primitive
                    new BHAVInstruction { Opcode = 0x04, TruePointer = 254, FalsePointer = 255, Operand = new byte[8] },
                },
            };
            iff.AddChunk(bhav);

            var dir = TestPaths.TempDir();
            var iffPath = Path.Combine(dir, "grabby.iff");
            using (var stream = new FileStream(iffPath, FileMode.Create)) iff.Write(stream);

            var result = PackCompilerApi.Decompile(iffPath, Path.Combine(dir, "out.json"));
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unsupported primitive opcode 0x04"));
            Assert.False(File.Exists(Path.Combine(dir, "out.json")));
        }
    }
}
