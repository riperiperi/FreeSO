using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class RoundTripTests
    {
        /// <summary>
        /// The critical proof: compile the example pack, re-read the emitted .iff with
        /// FSO.Files, and assert every BHAV instruction parses back to the intended
        /// opcode, pointers, and operand bytes (layouts per simantics-vocabulary.md,
        /// matching the engine operand Read() methods byte for byte).
        /// </summary>
        [Fact]
        public void GossipGnome_RoundTrips()
        {
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(TestPaths.Example("gossip-gnome.json"), outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iffPath = Path.Combine(outDir, "gossip_gnome.iff");
            Assert.True(File.Exists(iffPath));
            var iff = new IffFile(iffPath);

            // ---- OBJD ----
            var objd = iff.List<OBJD>().Single();
            Assert.Equal(0x6B4F0001u, objd.GUID);
            Assert.Equal(120, objd.Price);
            Assert.Equal(OBJDType.Normal, objd.ObjectType);
            Assert.Equal(1, objd.NumAttributes);
            Assert.Equal(128, objd.TreeTableID);
            Assert.Equal(4099, objd.BHAV_MainID); // main_loop
            Assert.Equal(4098, objd.BHAV_Init);   // init
            Assert.Equal(2000, objd.CatalogStringsID);
            Assert.Equal(129, objd.AnimationTableID);

            // ---- gossip_action (BHAV 4096) ----
            var action = iff.Get<BHAV>(4096);
            Assert.NotNull(action);
            Assert.Equal(0, action.Args);
            Assert.Equal(1, action.Locals); // dialog_roll
            Assert.Equal(7, action.Instructions.Length);

            // walk_over: goto_relative (0x1B) — OldTrapCount u16, Location sbyte (in_front_of=0),
            // Direction sbyte (facing=-2), RouteCount u16, Flags byte
            AssertInstruction(action.Instructions[0], 0x1B, t: 1, f: 255,
                operand: new byte[] { 0x00, 0x00, 0x00, 0xFE, 0x00, 0x00, 0x00, 0x00 });

            // check_ok: private tree call to gossip_test (opcode = its chunk id 4097),
            // VMSubRoutineOperand = four int16 args, all zero
            AssertInstruction(action.Instructions[1], 4097, t: 2, f: 255,
                operand: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            // chat_anim: animate (0x2C) — AnimationID 64, LocalEventNumber 0, pad,
            // Source person_stock=2, Flags 0, ExpectedEventCount 1
            AssertInstruction(action.Instructions[2], 0x2C, t: 3, f: 3,
                operand: new byte[] { 64, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x00 });

            // pick_line: random_number (0x08) — dest local[0] (scope 25 as u16!), range literal 2 (scope 7)
            AssertInstruction(action.Instructions[3], 0x08, t: 4, f: 253,
                operand: new byte[] { 0x00, 0x00, 25, 0x00, 0x02, 0x00, 0x07, 0x00 });

            // listen_dialog: dialog_private (0x24) — Message id 1, Type 0, Flags bit0 Continue
            AssertInstruction(action.Instructions[4], 0x24, t: 5, f: 5,
                operand: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01 });

            // reward: expression (0x02) — my_motives[social=14] += literal 15
            // LhsData i16, RhsData i16, IsSigned, Operator(+= is 3), LhsOwner 14, RhsOwner 7
            AssertInstruction(action.Instructions[5], 0x02, t: 6, f: 253,
                operand: new byte[] { 14, 0x00, 15, 0x00, 0x00, 0x03, 14, 0x07 });

            // count_it: expression — my_attributes[times_gossiped=0] += literal 1, then return true
            AssertInstruction(action.Instructions[6], 0x02, t: 254, f: 253,
                operand: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x00, 0x07 });

            // ---- gossip_test (BHAV 4097) ----
            var test = iff.Get<BHAV>(4097);
            Assert.NotNull(test);
            Assert.Single(test.Instructions);
            // literal 1 == literal 1 (Operator 2, both owners 7)
            AssertInstruction(test.Instructions[0], 0x02, t: 254, f: 255,
                operand: new byte[] { 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x07, 0x07 });

            // ---- init (BHAV 4098) ----
            var init = iff.Get<BHAV>(4098);
            AssertInstruction(init.Instructions[0], 0x02, t: 254, f: 253,
                operand: new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x07 }); // attr[0] = 0 (Assign=5)

            // ---- main_loop (BHAV 4099) ----
            var main = iff.Get<BHAV>(4099);
            // idle_for_input (0x11): StackVarToDec 0, AllowPush 1; loops to itself
            AssertInstruction(main.Instructions[0], 0x11, t: 0, f: 0,
                operand: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 });

            // ---- TTAB / TTAs ----
            var ttab = iff.Get<TTAB>(128);
            Assert.NotNull(ttab);
            var inter = ttab.Interactions.Single();
            Assert.Equal(4096, inter.ActionFunction);
            Assert.Equal(4097, inter.TestFunction);
            Assert.Equal(0u, inter.TTAIndex);
            Assert.Equal(16, inter.MotiveEntries.Length);
            Assert.Equal(20, inter.MotiveEntries[14].EffectRangeDelta); // social advertisement
            Assert.True(inter.AllowVisitors);
            Assert.True(inter.AllowObjectOwner);
            Assert.True(inter.AllowRoommates);
            Assert.False(inter.AllowGhosts);

            var ttas = iff.Get<TTAs>(128);
            Assert.Equal("Gossip", ttas.GetString(0));

            // ---- strings ----
            var dialog = iff.Get<STR>(301);
            Assert.Equal("The gnome listens intently.", dialog.GetString(0)); // dialog id 1 (1-based)
            Assert.Equal("Nice.", dialog.GetString(1));

            var ctss = iff.Get<CTSS>(2000);
            Assert.Equal("Gossip Gnome", ctss.GetString(0));

            var anim = iff.Get<STR>(129);
            Assert.Equal(1, anim.Length); // default empty animation table

            // ---- build report ----
            Assert.True(File.Exists(Path.Combine(outDir, "build-report.json")));
            Assert.Equal(4096, result.Report.Objects[0].Trees["gossip_action"]);
            Assert.Equal(0, result.Report.Objects[0].Attributes["times_gossiped"]);
        }

        /// <summary>
        /// Re-reading the emitted iff and writing it again must preserve every BHAV
        /// byte for byte. (Whole-file equality is not guaranteed: IffFile groups chunks
        /// by type on re-read, so chunk order can change.)
        /// </summary>
        [Fact]
        public void EmittedIff_IsStableAcrossRewrite()
        {
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(TestPaths.Example("gossip-gnome.json"), outDir);
            Assert.True(result.Success);

            var iffPath = Path.Combine(outDir, "gossip_gnome.iff");
            var iff = new IffFile(iffPath);
            var rewritten = Path.Combine(outDir, "rewritten.iff");
            using (var stream = new FileStream(rewritten, FileMode.Create)) iff.Write(stream);

            var reread = new IffFile(rewritten);
            for (ushort id = 4096; id <= 4099; id++)
            {
                var a = new IffFile(iffPath).Get<BHAV>(id);
                var b = reread.Get<BHAV>(id);
                Assert.Equal(a.Instructions.Length, b.Instructions.Length);
                for (int i = 0; i < a.Instructions.Length; i++)
                {
                    Assert.Equal(a.Instructions[i].Opcode, b.Instructions[i].Opcode);
                    Assert.Equal(a.Instructions[i].TruePointer, b.Instructions[i].TruePointer);
                    Assert.Equal(a.Instructions[i].FalsePointer, b.Instructions[i].FalsePointer);
                    Assert.Equal(a.Instructions[i].Operand, b.Instructions[i].Operand);
                }
            }
        }

        private static void AssertInstruction(BHAVInstruction inst, int opcode, int t, int f, byte[] operand)
        {
            Assert.Equal((ushort)opcode, inst.Opcode);
            Assert.Equal((byte)t, inst.TruePointer);
            Assert.Equal((byte)f, inst.FalsePointer);
            Assert.Equal(operand, inst.Operand);
        }
    }

    public static class TestPaths
    {
        public static string Example(string name)
        {
            // walk up from the test assembly to PackTools/examples
            var dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "examples", name)))
                dir = Path.GetDirectoryName(dir);
            if (dir == null) throw new FileNotFoundException("could not locate PackTools/examples/" + name);
            return Path.Combine(dir, "examples", name);
        }

        public static string TempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "fso-packc-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
