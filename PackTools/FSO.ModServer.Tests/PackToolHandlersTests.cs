using System;
using System.IO;
using System.Linq;
using FSO.ModServer;
using Newtonsoft.Json.Linq;

namespace FSO.ModServer.Tests
{
    public class SessionLifecycleTests
    {
        [Fact]
        public void CreatePack_ReturnsSessionId()
        {
            var result = JObject.FromObject(PackToolHandlers.CreatePack("gossip-gnome", "Gossip Gnome", "kat", "1.0.0", "desc"));
            Assert.True((bool)result["ok"]);
            Assert.False(string.IsNullOrEmpty((string)result["pack_session_id"]));
        }

        [Fact]
        public void ReadPack_UnknownSession_ReturnsUnknownSessionError()
        {
            var result = JObject.FromObject(PackToolHandlers.ReadPack("no-such-session"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_session", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void AddObject_UnknownSession_ReturnsUnknownSessionError()
        {
            var result = JObject.FromObject(PackToolHandlers.AddObject("no-such-session", "x", "X"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_session", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void AddObject_OmittedGuid_IsDeterministicAcrossSessions()
        {
            // Two independent sessions (as if two different processes/machines), same
            // pack id and object id, must allocate the same GUID with no shared state.
            var session1 = (string)JObject.FromObject(PackToolHandlers.CreatePack("teapot-pack", "Teapot Pack")).Value<string>("pack_session_id");
            var session2 = (string)JObject.FromObject(PackToolHandlers.CreatePack("teapot-pack", "Teapot Pack")).Value<string>("pack_session_id");

            var result1 = JObject.FromObject(PackToolHandlers.AddObject(session1, "teapot", "Teapot"));
            var result2 = JObject.FromObject(PackToolHandlers.AddObject(session2, "teapot", "Teapot"));

            Assert.Equal((string)result1["guid"], (string)result2["guid"]);
        }

        [Fact]
        public void AddObject_OmittedGuid_DifferentObjectId_AllocatesDifferentGuid()
        {
            var sessionId = (string)JObject.FromObject(PackToolHandlers.CreatePack("multi-object-pack", "Multi")).Value<string>("pack_session_id");

            var a = JObject.FromObject(PackToolHandlers.AddObject(sessionId, "object_a", "Object A"));
            var b = JObject.FromObject(PackToolHandlers.AddObject(sessionId, "object_b", "Object B"));

            Assert.NotEqual((string)a["guid"], (string)b["guid"]);
        }

        [Fact]
        public void AddObject_ManualGuidCollidesWithAutoAllocated_CaughtByValidate()
        {
            // The compiler's existing GUID-collision check is the backstop for allocator
            // collisions — prove it still fires when a second object's hand-picked guid
            // collides with the first object's auto-allocated one.
            var sessionId = (string)JObject.FromObject(PackToolHandlers.CreatePack("collision-pack", "Collision")).Value<string>("pack_session_id");

            var auto = JObject.FromObject(PackToolHandlers.AddObject(sessionId, "object_a", "Object A", category: "decorative", cloneFromGuid: "0xC14849AC"));
            var autoGuid = (string)auto["guid"];

            PackToolHandlers.AddObject(sessionId, "object_b", "Object B", category: "decorative", guid: autoGuid, cloneFromGuid: "0xC14849AC");

            var result = JObject.FromObject(PackToolHandlers.Validate(sessionId));
            Assert.False((bool)result["ok"]);
            Assert.Equal("guid_collision", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void EditTreeNode_UnknownTree_ReturnsUnknownTreeError()
        {
            var sessionId = (string)JObject.FromObject(PackToolHandlers.CreatePack("p", "P")).Value<string>("pack_session_id");
            PackToolHandlers.AddObject(sessionId, "obj1", "Obj1");

            var result = JObject.FromObject(PackToolHandlers.EditTreeNode(sessionId, "obj1", "no_such_tree",
                "{\"id\":\"n\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"literal\",\"value\":1},\"op\":\"==\",\"rhs\":{\"scope\":\"literal\",\"value\":1},\"then\":\"return true\",\"else\":\"return false\"}"));

            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_tree", (string)result["errors"][0]["code"]);
            Assert.Equal("obj1", (string)result["errors"][0]["object_id"]);
        }
    }

    public class VocabularyTests
    {
        [Fact]
        public void ListVocabulary_Primitives_ReturnsData()
        {
            var result = JObject.FromObject(PackToolHandlers.ListVocabulary("primitives"));
            Assert.True((bool)result["ok"]);
            Assert.True(result["data"].HasValues);
            Assert.NotNull(result["data"]["expression"]);
        }

        [Fact]
        public void ListVocabulary_Scopes_IncludesMyMotives()
        {
            var result = JObject.FromObject(PackToolHandlers.ListVocabulary("scopes"));
            Assert.Equal(14, (int)result["data"]["my_motives"]);
        }

        [Fact]
        public void ListVocabulary_UnknownKind_ReturnsErrorWithExpectedList()
        {
            var result = JObject.FromObject(PackToolHandlers.ListVocabulary("nonsense"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_vocabulary_kind", (string)result["errors"][0]["code"]);
            Assert.True(((JArray)result["errors"][0]["expected"]).Count > 0);
        }
    }

    public class NotImplementedStubTests
    {
        [Fact]
        public void DecompileObject_ReturnsErrorForMissingFile()
        {
            var result = JObject.FromObject(PackToolHandlers.DecompileObject("/no/such/object.iff"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("compile_error", (string)result["errors"][0]["code"]);
        }
    }

    /// <summary>End-to-end: build the gossip-gnome example one tool call at a time, then compile it for real.</summary>
    public class EndToEndFlowTests
    {
        private static string CreateSession()
        {
            var created = JObject.FromObject(PackToolHandlers.CreatePack("gossip-gnome", "Gossip Gnome", "kat", "1.0.0", "A garden gnome Sims can talk to."));
            return (string)created["pack_session_id"];
        }

        private static void BuildGossipGnome(string sessionId)
        {
            PackToolHandlers.AddObject(sessionId, "gossip_gnome", "Gossip Gnome",
                price: 120, category: "decorative", guid: "0x6B4F0001", cloneFromGuid: "0xC14849AC",
                attributesJson: "[\"times_gossiped\"]", entryMain: "main_loop", entryInit: "init");

            PackToolHandlers.AddInteraction(sessionId, "gossip_gnome",
                "{\"name\":\"Gossip\",\"action\":\"gossip_action\",\"test\":\"gossip_test\",\"allow\":{\"visitors\":true,\"owner\":true}}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "gossip_test");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_test",
                "{\"id\":\"always\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"literal\",\"value\":1},\"op\":\"==\",\"rhs\":{\"scope\":\"literal\",\"value\":1},\"then\":\"return true\",\"else\":\"return false\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "gossip_action", localsJson: "[\"dialog_roll\"]");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"walk_over\",\"prim\":\"goto_relative\",\"location\":\"in_front_of\",\"direction\":\"facing\",\"then\":\"chat_anim\",\"else\":\"return false\"}");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"chat_anim\",\"prim\":\"animate\",\"animation\":{\"source\":\"person_stock\",\"id\":64},\"expected_event_count\":1,\"then\":\"reward\",\"else\":\"reward\"}");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"reward\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_motives\",\"name\":\"social\"},\"op\":\"+=\",\"rhs\":{\"scope\":\"literal\",\"value\":15},\"then\":\"count_it\",\"else\":\"error\"}");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"count_it\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_attributes\",\"name\":\"times_gossiped\"},\"op\":\"+=\",\"rhs\":{\"scope\":\"literal\",\"value\":1},\"then\":\"return true\",\"else\":\"error\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "init");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "init",
                "{\"id\":\"zero_count\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_attributes\",\"name\":\"times_gossiped\"},\"op\":\"=\",\"rhs\":{\"scope\":\"literal\",\"value\":0},\"then\":\"return true\",\"else\":\"error\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "main_loop");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "main_loop",
                "{\"id\":\"idle\",\"prim\":\"idle_for_input\",\"ticks_param\":0,\"allow_push\":true,\"then\":\"idle\",\"else\":\"idle\"}");
        }

        [Fact]
        public void ReadPack_AfterEdits_ReflectsAllChanges()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var read = JObject.FromObject(PackToolHandlers.ReadPack(sessionId));
            var pack = (JObject)read["pack"];
            Assert.Equal("gossip_gnome", (string)pack["objects"][0]["id"]);
            Assert.Equal(4, ((JObject)pack["objects"][0]["trees"]).Properties().Count());
        }

        [Fact]
        public void Validate_WellFormedPack_Succeeds()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.Validate(sessionId));
            Assert.True((bool)result["ok"], result.ToString());
        }

        [Fact]
        public void Compile_WellFormedPack_EmitsRealIff()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.Compile(sessionId));
            Assert.True((bool)result["ok"], result.ToString());

            var outDir = (string)result["out_dir"];
            Assert.True(Directory.Exists(outDir));
            var iffPath = Path.Combine(outDir, "gossip_gnome.iff");
            Assert.True(File.Exists(iffPath));
            Assert.True(new FileInfo(iffPath).Length > 0);

            var report = result["report"];
            Assert.Equal("gossip-gnome", (string)report["packId"] ?? (string)report["PackId"]);
        }

        [Fact]
        public void Compile_BadEdit_ReturnsScopedErrorEnvelope()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            // corrupt one node: typo a field name, exactly the "did you mean" scenario MCP-DESIGN.md calls out
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"reward\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_motives\",\"name\":\"social\"},\"opp\":\"+=\",\"rhs\":{\"scope\":\"literal\",\"value\":15},\"then\":\"count_it\",\"else\":\"error\"}");

            var result = JObject.FromObject(PackToolHandlers.Validate(sessionId));
            Assert.False((bool)result["ok"]);

            var error = (JObject)result["errors"][0];
            Assert.Equal("gossip_gnome", (string)error["object_id"]);
            Assert.Equal("gossip_action", (string)error["tree_name"]);
            Assert.Equal("reward", (string)error["node_id"]);
        }

        [Fact]
        public void Compile_ErrorsPreventEmit()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"walk_over\",\"prim\":\"goto_relative\",\"location\":\"in_front_of\",\"direction\":\"facing\",\"then\":\"no_such_node\",\"else\":\"return false\"}");

            var result = JObject.FromObject(PackToolHandlers.Compile(sessionId));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unresolved_label", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void RemoveTreeNode_WithDanglingReference_WarnsNotErrors()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.RemoveTreeNode(sessionId, "gossip_gnome", "gossip_action", "count_it"));
            Assert.True((bool)result["ok"]);
            Assert.True(((JArray)result["warnings"]).Count > 0);
            Assert.Equal("dangling_reference", (string)result["warnings"][0]["code"]);
        }

        [Fact]
        public void SetDialogString_UnknownObject_ReturnsUnknownObjectError()
        {
            var sessionId = CreateSession();
            var result = JObject.FromObject(PackToolHandlers.SetDialogString(sessionId, "no_such_object", 1, "hi"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_object", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void SetDialogString_IndexOutOfRange_ReturnsError()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            var result = JObject.FromObject(PackToolHandlers.SetDialogString(sessionId, "gossip_gnome", 256, "hi"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("invalid_field_value", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void SetDialogString_LandsInPackJson()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var setResult = JObject.FromObject(PackToolHandlers.SetDialogString(sessionId, "gossip_gnome", 1, "The gnome listens intently."));
            Assert.True((bool)setResult["ok"], setResult.ToString());

            var read = JObject.FromObject(PackToolHandlers.ReadPack(sessionId));
            var pack = (JObject)read["pack"];
            var dialog = (JObject)pack["objects"][0]["strings"]["dialog"];
            Assert.Equal("The gnome listens intently.", (string)dialog["1"]);
        }

        [Fact]
        public void SetDialogString_ThenCompile_RoundTripsThroughDecompile()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            PackToolHandlers.SetDialogString(sessionId, "gossip_gnome", 1, "The gnome listens intently.");
            PackToolHandlers.SetDialogString(sessionId, "gossip_gnome", 2, "Nice.");

            var compileResult = JObject.FromObject(PackToolHandlers.Compile(sessionId));
            Assert.True((bool)compileResult["ok"], compileResult.ToString());

            var outDir = (string)compileResult["out_dir"];
            var iffPath = Path.Combine(outDir, "gossip_gnome.iff");
            Assert.True(File.Exists(iffPath));

            var decompiled = JObject.FromObject(PackToolHandlers.DecompileObject(iffPath));
            Assert.True((bool)decompiled["ok"], decompiled.ToString());

            var dialog = (JObject)decompiled["pack"]["objects"][0]["strings"]["dialog"];
            Assert.Equal("The gnome listens intently.", (string)dialog["1"]);
            Assert.Equal("Nice.", (string)dialog["2"]);
        }

        [Fact]
        public void AddTree_Duplicate_ReturnsError()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.AddTree(sessionId, "gossip_gnome", "gossip_action"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("duplicate_tree", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void TestInVm_UnknownSession_ReturnsUnknownSessionError()
        {
            var result = JObject.FromObject(PackToolHandlers.TestInVm("no-such-session", "{}"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_session", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void TestInVm_InvalidScenarioJson_ReturnsInvalidJsonError()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId, "{not json"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("invalid_json", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void TestInVm_CompileFails_ReturnsCompileErrorEnvelope()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"walk_over\",\"prim\":\"goto_relative\",\"location\":\"in_front_of\",\"direction\":\"facing\",\"then\":\"no_such_node\",\"else\":\"return false\"}");

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId, "{}"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unresolved_label", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void TestInVm_UnknownPlaceObject_ReturnsUnknownObjectError()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId, "{\"place_object\":\"no_such_object\"}"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("unknown_object", (string)result["errors"][0]["code"]);
        }

        /// <summary>
        /// True if this checkout has real TSO game content on disk and a built FSO.VMHarness —
        /// the only two things the tests below actually need to run the subprocess for real.
        /// Callers no-op (rather than fail) when this is false, since neither is something
        /// CI/a fresh clone can be expected to have.
        /// </summary>
        private static bool HasRealVmHarnessFixture(out JObject probeResult, string sessionId)
        {
            var gameLocation = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            if (!Directory.Exists(gameLocation))
            {
                probeResult = null;
                return false;
            }

            probeResult = JObject.FromObject(PackToolHandlers.TestInVm(sessionId, "{\"max_ticks\":1}"));
            if (!(bool)probeResult["ok"] && (string)probeResult["errors"][0]["code"] == "vm_harness_not_built")
                return false;
            return true;
        }

        /// <summary>
        /// Full run against real TSO game content and a built FSO.VMHarness — the only test
        /// that exercises the actual subprocess. No-ops (rather than failing) on a checkout
        /// that lacks either, since neither is something CI/a fresh clone can be expected to have.
        /// </summary>
        [Fact]
        public void TestInVm_RealGameContent_RunsAndReturnsTrace()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            if (!HasRealVmHarnessFixture(out _, sessionId)) return;

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId,
                "{\"place_object\":\"gossip_gnome\",\"push_interaction\":\"Gossip\",\"max_ticks\":200," +
                "\"assertions\":[{\"type\":\"motive_at_least\",\"target\":\"sim\",\"motive\":\"social\",\"value\":10}]}"));

            Assert.True((bool)result["ok"], result.ToString());
            Assert.Equal("Gossip", (string)result["pushed_interaction"]);
            Assert.True(((JArray)result["trace"]).Count > 0);
            Assert.True((int)result["final_state"]["sim_motive_social"] >= 10);

            var assertion = (JObject)result["assertions"][0];
            Assert.True((bool)assertion["passed"]);
        }

        /// <summary>test_in_vm compiles the pack itself — MCP-DESIGN.md §3: "an agent shouldn't
        /// need to remember to call both [compile and test_in_vm] in the right order."</summary>
        [Fact]
        public void TestInVm_NeverCalledCompile_StillCompilesAndRuns()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            if (!HasRealVmHarnessFixture(out var probe, sessionId)) return;

            // the probe call itself is the proof: this session never called Compile,
            // yet TestInVm produced a real trace off a freshly built .iff.
            Assert.True((bool)probe["ok"], probe.ToString());
            Assert.NotNull(probe["pushed_interaction"]);
        }

        /// <summary>
        /// Builds a gnome whose Gossip action has no routing/animation (runs to completion in
        /// one tick headless), SETS social to 37 (motives start at the 100 cap, so += deltas
        /// are invisible — a distinctive set value is the only observable motive change), and
        /// bumps the OBJECT's attribute via stack_object_attributes ("my" scopes in an
        /// interaction resolve against the caller avatar, not the object).
        /// </summary>
        private static void BuildAssertableGnome(string sessionId)
        {
            PackToolHandlers.AddObject(sessionId, "gossip_gnome", "Gossip Gnome",
                price: 120, category: "decorative", guid: "0x6B4F0001", cloneFromGuid: "0xC14849AC",
                attributesJson: "[\"times_gossiped\"]", entryMain: "main_loop", entryInit: "init");
            PackToolHandlers.AddInteraction(sessionId, "gossip_gnome",
                "{\"name\":\"Gossip\",\"action\":\"gossip_action\",\"test\":\"gossip_test\",\"allow\":{\"visitors\":true,\"owner\":true}}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "gossip_test");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_test",
                "{\"id\":\"always\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"literal\",\"value\":1},\"op\":\"==\",\"rhs\":{\"scope\":\"literal\",\"value\":1},\"then\":\"return true\",\"else\":\"return false\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "gossip_action");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"reward\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_motives\",\"name\":\"social\"},\"op\":\"=\",\"rhs\":{\"scope\":\"literal\",\"value\":37},\"then\":\"count_it\",\"else\":\"error\"}");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "gossip_action",
                "{\"id\":\"count_it\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"stack_object_attributes\",\"name\":\"times_gossiped\"},\"op\":\"+=\",\"rhs\":{\"scope\":\"literal\",\"value\":1},\"then\":\"return true\",\"else\":\"error\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "init");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "init",
                "{\"id\":\"zero_count\",\"prim\":\"expression\",\"lhs\":{\"scope\":\"my_attributes\",\"name\":\"times_gossiped\"},\"op\":\"=\",\"rhs\":{\"scope\":\"literal\",\"value\":0},\"then\":\"return true\",\"else\":\"error\"}");

            PackToolHandlers.AddTree(sessionId, "gossip_gnome", "main_loop");
            PackToolHandlers.EditTreeNode(sessionId, "gossip_gnome", "main_loop",
                "{\"id\":\"idle\",\"prim\":\"idle_for_input\",\"ticks_param\":0,\"allow_push\":true,\"then\":\"idle\",\"else\":\"idle\"}");
        }

        [Fact]
        public void TestInVm_MotiveAssertions_AtLeastAtMostEquals_EvaluateAgainstFinalMotive()
        {
            var sessionId = CreateSession();
            BuildAssertableGnome(sessionId);
            if (!HasRealVmHarnessFixture(out _, sessionId)) return;

            // Gossip's reward node sets social to exactly 37.
            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId,
                "{\"push_interaction\":\"Gossip\",\"max_ticks\":200,\"assertions\":[" +
                "{\"type\":\"motive_at_least\",\"target\":\"sim\",\"motive\":\"social\",\"value\":10}," +
                "{\"type\":\"motive_at_most\",\"target\":\"sim\",\"motive\":\"social\",\"value\":50}," +
                "{\"type\":\"motive_equals\",\"target\":\"sim\",\"motive\":\"social\",\"value\":37}," +
                "{\"type\":\"motive_equals\",\"target\":\"sim\",\"motive\":\"social\",\"value\":999}" +
                "]}"));

            Assert.True((bool)result["ok"], result.ToString());
            var assertions = (JArray)result["assertions"];
            Assert.True((bool)assertions[0]["passed"], result.ToString());
            Assert.True((bool)assertions[1]["passed"], result.ToString());
            Assert.True((bool)assertions[2]["passed"], result.ToString());
            Assert.False((bool)assertions[3]["passed"]);
            Assert.Equal(37.0, (double)assertions[3]["actual"]);
        }

        [Fact]
        public void TestInVm_AttributeEqualsAssertion_EvaluatesAgainstFinalAttribute()
        {
            var sessionId = CreateSession();
            BuildAssertableGnome(sessionId);
            if (!HasRealVmHarnessFixture(out _, sessionId)) return;

            // count_it bumps the gnome's times_gossiped (attribute index 0) 0 -> 1 via stack_object_attributes.
            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId,
                "{\"push_interaction\":\"Gossip\",\"max_ticks\":200,\"assertions\":[" +
                "{\"type\":\"attribute_equals\",\"target\":\"gossip_gnome\",\"attribute\":\"times_gossiped\",\"value\":1}]}"));

            Assert.True((bool)result["ok"], result.ToString());
            var assertion = (JObject)result["assertions"][0];
            Assert.True((bool)assertion["passed"], result.ToString());
            Assert.Equal(1L, (long)assertion["actual"]);
        }

        [Fact]
        public void TestInVm_UnsupportedAssertionTypes_ReturnNullPassedWithWarning()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            if (!HasRealVmHarnessFixture(out _, sessionId)) return;

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId,
                "{\"push_interaction\":\"Gossip\",\"max_ticks\":200,\"assertions\":[" +
                "{\"type\":\"node_reached\",\"target\":\"gossip_gnome\",\"node\":\"reward\"}," +
                "{\"type\":\"node_not_reached\",\"target\":\"gossip_gnome\",\"node\":\"walk_over\"}," +
                "{\"type\":\"tree_returned\",\"target\":\"gossip_gnome\",\"tree\":\"gossip_action\"}]}"));

            Assert.True((bool)result["ok"], result.ToString());
            var assertions = (JArray)result["assertions"];
            Assert.Equal(3, assertions.Count);
            foreach (var a in assertions)
                Assert.Equal(JTokenType.Null, a["passed"].Type);

            var warnings = (JArray)result["warnings"];
            Assert.Equal(3, warnings.Count(w => (string)w["code"] == "assertion_not_evaluable"));
        }

        [Fact]
        public void TestInVm_SpawnSimField_ReturnsUnsupportedScenarioFieldWarning()
        {
            var sessionId = CreateSession();
            BuildGossipGnome(sessionId);
            if (!HasRealVmHarnessFixture(out _, sessionId)) return;

            var result = JObject.FromObject(PackToolHandlers.TestInVm(sessionId,
                "{\"push_interaction\":\"Gossip\",\"max_ticks\":200,\"spawn_sim\":{\"motives\":{\"social\":0}}}"));

            Assert.True((bool)result["ok"], result.ToString());
            var warning = ((JArray)result["warnings"]).Single(w => (string)w["code"] == "unsupported_scenario_field");
            Assert.Equal("spawn_sim", (string)warning["field"]);
        }
    }

    public class FindBaseObjectTests
    {
        // The game content isn't present on every machine (CI has no TSO install), so the
        // content-dependent assertions skip rather than fail. The behaviours that don't need
        // content are always checked.
        private static string GameDir
        {
            get
            {
                var dir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION");
                if (string.IsNullOrEmpty(dir))
                    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library/Application Support/The Sims Online/TSOClient");
                return dir;
            }
        }

        private static bool HasGameContent =>
            File.Exists(FSO.PackCompiler.BaseObjectIndex.TablePath(GameDir));

        [Fact]
        public void EmptyQuery_ReturnsInvalidQueryError()
        {
            var result = JObject.FromObject(PackToolHandlers.FindBaseObject("   "));
            Assert.False((bool)result["ok"]);
            Assert.Equal("invalid_query", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void BadGameLocation_ReturnsGameContentMissingError()
        {
            var result = JObject.FromObject(PackToolHandlers.FindBaseObject("gnome", 8,
                Path.Combine(Path.GetTempPath(), "definitely-not-a-tso-install")));
            Assert.False((bool)result["ok"]);
            Assert.Equal("game_content_missing", (string)result["errors"][0]["code"]);
        }

        [Fact]
        public void KnownObject_ReturnsRealClonableGuid()
        {
            if (!HasGameContent) return;

            var result = JObject.FromObject(PackToolHandlers.FindBaseObject("garden gnome"));
            Assert.True((bool)result["ok"], result.ToString());

            var candidates = (JArray)result["candidates"];
            Assert.NotEmpty(candidates);
            // The base-game Garden Gnome, independently confirmed as clonable by the
            // AgentBridge dispatch-test fixture.
            Assert.Contains(candidates, c => (string)c["guid"] == "0xC14849AC");
        }

        [Fact]
        public void LimitIsRespected()
        {
            if (!HasGameContent) return;

            var result = JObject.FromObject(PackToolHandlers.FindBaseObject("chair", 3));
            Assert.True((bool)result["ok"], result.ToString());
            Assert.Equal(3, ((JArray)result["candidates"]).Count);
        }

        [Fact]
        public void NoMatch_TellsTheAgentToUseGeneratedRatherThanSkipAppearance()
        {
            if (!HasGameContent) return;

            var result = JObject.FromObject(PackToolHandlers.FindBaseObject("zzzznotathing"));
            Assert.False((bool)result["ok"]);
            Assert.Equal("no_base_object_match", (string)result["errors"][0]["code"]);

            // The whole point of the miss path: leaving appearance off compiles clean and
            // renders an invisible object, so the message must not let that read as an option.
            var message = (string)result["errors"][0]["message"];
            Assert.Contains("appearance.generated", message);
            Assert.Contains("invisible", message);
        }
    }
}
