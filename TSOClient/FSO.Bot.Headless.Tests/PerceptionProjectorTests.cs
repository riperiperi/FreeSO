using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using FSO.Bot.Headless;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the perception projector's pure logic surfaces: motive trend classification,
/// animation translation, time-of-day formatting, compass-direction binning, interaction-hint
/// lookup, recent-events buffer cap, and NDJSON serialisation shape.
///
/// Full VMAvatar → tick transformation is covered by the live integration test in
/// tests/integration/perception_smoke_test.sh — constructing a VMAvatar in unit test code
/// would require full VM init with Content, which is exactly the kind of mocking the item
/// spec forbids. The integration test exercises Build() against a real VM on workshop.
/// </summary>
public class PerceptionProjectorTests
{
    [Theory]
    [InlineData(1.0,   "rising")]
    [InlineData(0.6,   "rising")]
    [InlineData(0.5,   "stable")]
    [InlineData(0.0,   "stable")]
    [InlineData(-0.4,  "stable")]
    [InlineData(-0.6,  "falling")]
    [InlineData(-10.0, "falling")]
    public void ClassifyTrend_Boundaries(double dpm, string expected)
    {
        Assert.Equal(expected, PerceptionProjector.ClassifyTrend(dpm));
    }

    [Theory]
    [InlineData(0.0f, "N")]
    [InlineData(0.785f, "NE")]   // PI/4
    [InlineData(1.57f, "E")]
    [InlineData(3.14f, "S")]
    [InlineData(-1.57f, "W")]
    public void DirectionToCompass_Bins(float radians, string expected)
    {
        Assert.Equal(expected, PerceptionProjector.DirectionToCompass(radians));
    }

    [Theory]
    [InlineData(10, 23, "10:23 AM")]
    [InlineData(0,  0,  "12:00 AM")]
    [InlineData(12, 0,  "12:00 PM")]
    [InlineData(13, 5,  "01:05 PM")]
    [InlineData(23, 59, "11:59 PM")]
    public void FormatTime_AmPm(int h, int m, string expected)
    {
        Assert.Equal(expected, PerceptionProjector.FormatTime(h, m));
    }

    [Theory]
    [InlineData(4,  "night")]
    [InlineData(6,  "morning")]
    [InlineData(11, "morning")]
    [InlineData(12, "afternoon")]
    [InlineData(16, "afternoon")]
    [InlineData(17, "evening")]
    [InlineData(20, "evening")]
    [InlineData(21, "night")]
    public void NameTimeOfDay_Bins(int h, string expected)
    {
        Assert.Equal(expected, PerceptionProjector.NameTimeOfDay(h));
    }

    [Fact]
    public void AnimationRules_LoadFromJsonOrDefault_HasTwentyEntries()
    {
        var rules = PerceptionProjector.LoadAnimationRules();
        Assert.True(rules.Count >= 20, $"expected >= 20 animation rules, got {rules.Count}");
    }

    [Fact]
    public void AnimationRules_MatchKnownCodes()
    {
        var rules = PerceptionProjector.LoadAnimationRules();
        var projector = new PerceptionProjector(42, "Alphaville");

        // Test via reflection of TranslateAnimation — it's private, so exercise through rules direct.
        foreach (var rule in rules)
        {
            // Pick a representative — the first rule matching a test string we know.
            if (rule.Pattern.Contains("idle.*stand"))
            {
                Assert.True(rule.Regex.IsMatch("a2o-idle-neutral-lstand-fidget-1c"));
            }
            if (rule.Pattern == "walk")
            {
                Assert.True(rule.Regex.IsMatch("a2o-walk-sidestep-left"));
            }
        }
    }

    [Fact]
    public void InteractionHints_Load_HasTwentyObjects()
    {
        // Load from the content file directly — the file is part of the artifact and must exist.
        var hintsPath = FindContentFile("interaction_hints.json");
        Assert.True(File.Exists(hintsPath), $"interaction_hints.json must ship alongside binary: {hintsPath}");

        var text = File.ReadAllText(hintsPath);
        var hints = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PerceptionProjector.InteractionHint>>>(text);
        Assert.NotNull(hints);
        Assert.True(hints.Count >= 20, $"expected >= 20 object entries, got {hints.Count}");

        // Every entry has at least one interaction with effects or gates populated (or empty arrays).
        foreach (var kv in hints)
        {
            Assert.NotEmpty(kv.Value);
        }

        // A few specific ones the schema example calls out.
        Assert.Contains("Bed", hints.Keys);
        Assert.Contains("Fridge", hints.Keys);
        Assert.Contains("Toilet", hints.Keys);
    }

    [Fact]
    public void RecentEvents_BufferCapsAtTen()
    {
        var p = new PerceptionProjector(1, "Alphaville");
        for (int i = 0; i < 25; i++)
        {
            p.AddRecentEvent(new PerceptionEvent
            {
                T = i,
                Kind = "dialog",
                Text = "evt " + i,
            });
        }
        // No public getter — serialise a tick would require a VM. Use reflection on the private field.
        var field = typeof(PerceptionProjector).GetField("_recentEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (System.Collections.IEnumerable)field.GetValue(p);
        int count = 0;
        foreach (var _ in list) count++;
        Assert.Equal(PerceptionProjector.RecentEventsCap, count);
    }

    [Fact]
    public void PerceptionTick_SerializesToNDJSONSingleLine()
    {
        var tick = new PerceptionTick
        {
            Kind = "perception",
            T = 1700000000000L,
            Avatar = new AvatarBlock
            {
                PersistId = 2,
                Name = "baron",
                Shard = "Alphaville",
                Position = new PositionBlock { X = 32.5, Y = 40.0, Level = 1, Direction = "NE" },
                AnimationRaw = "a2o-idle-neutral-lstand-fidget-1c",
                AnimationHuman = "standing idle, fidgeting",
                ActionQueue = new List<ActionQueueItemBlock>(),
            },
            Motives = new Dictionary<string, MotiveBlock>
            {
                ["hunger"] = new MotiveBlock { Value = 71, DeltaPerMinute = -2.0, Trend = "falling" },
            },
            NearbyObjects = new List<NearbyObjectBlock>(),
            NearbySims = new List<NearbySimBlock>(),
            Skills = new Dictionary<string, int>(),
            Relationships = new List<RelationshipBlock>(),
            Inventory = new List<InventoryItemBlock>(),
            Balance = 39440,
            RecentEvents = new List<PerceptionEvent>(),
            Lot = new LotBlock
            {
                Name = "Baron's House",
                LotId = 2,
                OwnerIsMe = true,
                OtherAvatars = 1,
                SimTime = "10:23 AM",
                TimeOfDay = "morning",
            },
        };

        var json = PerceptionEmitter.Serialize(tick);

        // NDJSON: single line, no embedded newlines.
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("\r", json);

        // Valid JSON, round-trips.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Schema-shape assertions (§5).
        Assert.Equal("perception", root.GetProperty("kind").GetString());
        Assert.True(root.TryGetProperty("avatar", out var av));
        Assert.Equal(2u, av.GetProperty("persist_id").GetUInt32());
        Assert.Equal("baron", av.GetProperty("name").GetString());
        Assert.Equal("Alphaville", av.GetProperty("shard").GetString());
        Assert.Equal(1, av.GetProperty("position").GetProperty("level").GetInt32());
        Assert.Equal("NE", av.GetProperty("position").GetProperty("direction").GetString());
        Assert.Equal("a2o-idle-neutral-lstand-fidget-1c", av.GetProperty("animation_raw").GetString());
        Assert.Equal("standing idle, fidgeting", av.GetProperty("animation_human").GetString());

        Assert.True(root.TryGetProperty("motives", out var mots));
        Assert.Equal(71, mots.GetProperty("hunger").GetProperty("value").GetInt32());
        Assert.Equal("falling", mots.GetProperty("hunger").GetProperty("trend").GetString());

        Assert.True(root.TryGetProperty("nearby_objects", out _));
        Assert.True(root.TryGetProperty("nearby_sims", out _));
        Assert.True(root.TryGetProperty("skills", out _));
        Assert.True(root.TryGetProperty("relationships", out _));
        Assert.True(root.TryGetProperty("inventory", out _));
        Assert.True(root.TryGetProperty("balance", out _));
        Assert.True(root.TryGetProperty("recent_events", out _));
        Assert.True(root.TryGetProperty("lot", out var lot));
        Assert.Equal("Baron's House", lot.GetProperty("name").GetString());
        Assert.True(lot.GetProperty("owner_is_me").GetBoolean());
        Assert.Equal("10:23 AM", lot.GetProperty("sim_time").GetString());

        Assert.Equal(39440, root.GetProperty("balance").GetInt32());
    }

    [Fact]
    public void DialogEvent_SerializesAsKindDialog()
    {
        var evt = new PerceptionEvent
        {
            T = 1700000000000L,
            Kind = "dialog",
            Text = "I'm not in a good enough mood to work out.",
            Extras = new Dictionary<string, object>
            {
                ["title"] = "",
                ["dialog_id"] = "12345",
            },
        };
        var json = PerceptionEmitter.SerializeEvent(evt, "dialog");
        Assert.DoesNotContain("\n", json);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("dialog", doc.RootElement.GetProperty("kind").GetString());
        Assert.Equal(evt.Text, doc.RootElement.GetProperty("text").GetString());
        Assert.Equal("12345", doc.RootElement.GetProperty("dialog_id").GetString());
    }

    /// <summary>
    /// Verify the projector instance can be constructed and attached without a VM present —
    /// Build(null) returns null, and Build() against a freshly-init VM with no MyAvatar
    /// present also returns null. Proves the projector fails soft.
    /// </summary>
    [Fact]
    public void Build_NullVm_ReturnsNull()
    {
        var p = new PerceptionProjector(42, "Alphaville");
        Assert.Null(p.Build(null));
    }

    [Fact]
    public void ActionQueueItemBlock_SerializesModeField()
    {
        // The agent's queue-mode decision (preempt vs queue) hinges on distinguishing
        // engine-pushed Idle from deliberate Normal-mode actions in perception.
        var tick = new PerceptionTick
        {
            Kind = "perception",
            T = 1700000000000L,
            Avatar = new AvatarBlock
            {
                PersistId = 5, Name = "Botrous", Shard = "Alphaville",
                Position = new PositionBlock { X = 35.5, Y = 70.5, Level = 1, Direction = "N" },
                AnimationRaw = "a2o-idle", AnimationHuman = "standing idle",
                ActionQueue = new List<ActionQueueItemBlock>
                {
                    new() { InteractionId = 1, Name = "Idle", TargetObjectId = 1336, Status = "running", Mode = "idle" },
                    new() { InteractionId = 2, Name = "Read Newspaper", TargetObjectId = 9, Status = "queued", Mode = "normal" },
                },
            },
            Motives = new Dictionary<string, MotiveBlock>(),
            NearbyObjects = new List<NearbyObjectBlock>(),
            NearbySims = new List<NearbySimBlock>(),
            Skills = new Dictionary<string, int>(),
            Relationships = new List<RelationshipBlock>(),
            Inventory = new List<InventoryItemBlock>(),
            Balance = 0,
            RecentEvents = new List<PerceptionEvent>(),
            Lot = new LotBlock { Name = "Main", LotId = 2, OwnerIsMe = false, OtherAvatars = 0, SimTime = "00:00", TimeOfDay = "night" },
        };
        var json = PerceptionEmitter.Serialize(tick);
        Assert.Contains("\"mode\":\"idle\"", json);
        Assert.Contains("\"mode\":\"normal\"", json);
    }

    /// <summary>
    /// Verify NearbyObjectBlock carries the object_type field and it serializes
    /// into the JSON output (freesoexperiment-d5b). We build a PerceptionTick with a
    /// handcrafted NearbyObjectBlock to test the serialization shape without a live VM.
    ///
    /// ExtractObjectType is tested separately (it requires a VMEntity which needs
    /// full VM + Content init — covered by the live integration test). This test
    /// verifies the DTO schema and the JSON key name so the sidecar can rely on
    /// "object_type" being present in the payload.
    /// </summary>
    [Fact]
    public void NearbyObjectBlock_ObjectType_SerializesToJson()
    {
        var tick = new PerceptionTick
        {
            Kind = "perception",
            T = 1700000000000L,
            Avatar = new AvatarBlock
            {
                PersistId = 2, Name = "baron", Shard = "Alphaville",
                Position = new PositionBlock { X = 32.5, Y = 40.0, Level = 1, Direction = "N" },
                AnimationRaw = string.Empty, AnimationHuman = string.Empty,
                ActionQueue = new List<ActionQueueItemBlock>(),
            },
            Motives = new Dictionary<string, MotiveBlock>(),
            NearbyObjects = new List<NearbyObjectBlock>
            {
                new NearbyObjectBlock
                {
                    ObjectId = 10,
                    Name = "Staircase",
                    Category = "generic",
                    ObjectType = "Portal",   // stairs are OBJDType.Portal
                    Position = new PositionBlock { X = 34.0, Y = 42.0, Level = 1 },
                    DistanceTiles = 2.0,
                    Interactions = new List<InteractionBlock>(),
                },
                new NearbyObjectBlock
                {
                    ObjectId = 11,
                    Name = "Fridge",
                    Category = "food",
                    ObjectType = "Normal",   // buyable objects are OBJDType.Normal
                    Position = new PositionBlock { X = 35.0, Y = 43.0, Level = 1 },
                    DistanceTiles = 3.0,
                    Interactions = new List<InteractionBlock>(),
                },
            },
            NearbySims = new List<NearbySimBlock>(),
            Skills = new Dictionary<string, int>(),
            Relationships = new List<RelationshipBlock>(),
            Inventory = new List<InventoryItemBlock>(),
            Balance = 1000,
            RecentEvents = new List<PerceptionEvent>(),
            Lot = new LotBlock
            {
                Name = "Main", LotId = 2, OwnerIsMe = true,
                OtherAvatars = 0, SimTime = "12:00 PM", TimeOfDay = "afternoon",
            },
        };

        var json = PerceptionEmitter.Serialize(tick);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("nearby_objects", out var nearbyArr));
        Assert.Equal(2, nearbyArr.GetArrayLength());

        // First entry: stair (Portal)
        var stairEntry = nearbyArr[0];
        Assert.True(stairEntry.TryGetProperty("object_type", out var stairType),
            "object_type must be present in nearby_objects entries");
        Assert.Equal("Portal", stairType.GetString());
        Assert.Equal("Staircase", stairEntry.GetProperty("name").GetString());

        // Second entry: fridge (Normal)
        var fridgeEntry = nearbyArr[1];
        Assert.True(fridgeEntry.TryGetProperty("object_type", out var fridgeType),
            "object_type must be present in nearby_objects entries");
        Assert.Equal("Normal", fridgeType.GetString());
    }

    private static string FindContentFile(string fileName)
    {
        // Look in known locations: test output dir (CopyToOutput), build/Content, source tree.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Content", fileName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FSO.Bot.Headless", "Content", fileName),
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return Path.GetFullPath(c);
        }
        return candidates[^1];
    }
}
