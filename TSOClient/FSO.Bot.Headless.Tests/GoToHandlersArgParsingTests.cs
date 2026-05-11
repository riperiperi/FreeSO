/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Regression tests for freesoexperiment-f5e: cross-level go-to fails live because
/// cf sends type=json args as raw strings and GoToHandlers.GoTo did
/// <c>args["location"] as JsonObject</c>, which returns null for JsonString nodes.
///
/// <para>
/// <b>Root cause:</b> The cf client (executor.go <c>validateSingleValue</c>) validates
/// type=json args as strings. After <c>json.Marshal(resolved)</c> the wire payload is:
/// <c>"location":"{\"x\":42,\"y\":59,\"level\":2}"</c> — a JSON string, not a nested object.
/// <c>JsonNode.Parse</c> on the full payload yields a <c>JsonValue</c> for the location
/// field, not a <c>JsonObject</c>. The direct cast <c>as JsonObject</c> silently returns null,
/// causing the handler to fall through to the "requires one of…" error path.
/// </para>
///
/// <para>
/// <b>Fix:</b> <see cref="GoToHandlers.ParseJsonObjectArg"/> tries direct cast first
/// (future-proof if cf changes), then falls back to parsing the string value.
/// </para>
///
/// These tests exercise <see cref="GoToHandlers.ParseJsonObjectArg"/> directly (pure-logic,
/// no VM required) and also verify that the full wire payload — as deserialized by
/// <c>System.Text.Json</c> — produces the correct field types (JsonValue/string vs JsonObject).
/// </summary>
public class GoToHandlersArgParsingTests
{
    // ── ParseJsonObjectArg unit tests ──────────────────────────────────────────

    [Fact]
    public void ParseJsonObjectArg_NullNode_ReturnsNull()
    {
        var result = GoToHandlers.ParseJsonObjectArg(null);
        Assert.Null(result);
    }

    [Fact]
    public void ParseJsonObjectArg_DirectJsonObject_ReturnsSameObject()
    {
        // Future-proof path: if cf ever embeds the object directly instead of
        // serialising it as a string, we must still handle it.
        var obj = JsonNode.Parse("""{"x":42,"y":59,"level":2}""") as JsonObject;
        var result = GoToHandlers.ParseJsonObjectArg(obj);
        Assert.Same(obj, result);
    }

    /// <summary>
    /// Core regression: cf client sends type=json args as raw strings.
    /// A JsonValue node whose string content is a JSON object must be parsed.
    /// </summary>
    [Fact]
    public void ParseJsonObjectArg_JsonStringWithValidJson_ReturnsParsedObject()
    {
        // This is the current cf client behaviour: type=json arg arrives as a string node.
        // Simulate the wire value: a JsonValue whose string content is a JSON object.
        var stringNode = JsonValue.Create("""{"x":42,"y":59,"level":2}""");
        var result = GoToHandlers.ParseJsonObjectArg(stringNode);

        Assert.NotNull(result);
        Assert.Equal(42L, (long)result["x"]);
        Assert.Equal(59L, (long)result["y"]);
        Assert.Equal(2L, (long)result["level"]);
    }

    [Fact]
    public void ParseJsonObjectArg_JsonStringWithInvalidJson_ReturnsNull()
    {
        var stringNode = JsonValue.Create("not-json-at-all");
        var result = GoToHandlers.ParseJsonObjectArg(stringNode);
        Assert.Null(result);
    }

    [Fact]
    public void ParseJsonObjectArg_JsonStringWithJsonArray_ReturnsNull()
    {
        // A JSON string that is valid JSON but not an object.
        var stringNode = JsonValue.Create("[1,2,3]");
        var result = GoToHandlers.ParseJsonObjectArg(stringNode);
        Assert.Null(result);
    }

    [Fact]
    public void ParseJsonObjectArg_JsonStringWithLevelOmitted_ParsesXY()
    {
        // Omitting "level" from the location string — handler must default to 1.
        var stringNode = JsonValue.Create("""{"x":10,"y":20}""");
        var result = GoToHandlers.ParseJsonObjectArg(stringNode);

        Assert.NotNull(result);
        Assert.Equal(10L, (long)result["x"]);
        Assert.Equal(20L, (long)result["y"]);
        Assert.Null(result["level"]); // absent — handler applies default (1)
    }

    // ── Wire payload round-trip tests ─────────────────────────────────────────
    //
    // These tests verify that a payload serialised the way cf serialises it
    // (type=json arg stored as string, then json.Marshal'd to JSON) round-trips
    // correctly through ParseJsonObjectArg. This is the actual wire contract.

    /// <summary>
    /// Same-floor location: cf sends {"location":"{\"x\":42,\"y\":59,\"level\":1}"}
    /// ParseJsonObjectArg must yield x=42,y=59,level=1.
    /// </summary>
    [Fact]
    public void WirePayload_SameFloor_LocationStringParsesCorrectly()
    {
        // Simulate cf: the resolved args map has location as a Go string,
        // json.Marshal produces the outer object, location field is a JSON string.
        var outerPayload = """{"location":"{\"x\":42,\"y\":59,\"level\":1}"}""";
        var outer = JsonNode.Parse(outerPayload) as JsonObject;

        // The location node is a JsonValue (string kind), not a JsonObject.
        var locationNode = outer["location"];
        Assert.IsNotType<JsonObject>(locationNode); // confirm the bug scenario: direct cast fails

        var parsed = GoToHandlers.ParseJsonObjectArg(locationNode);

        Assert.NotNull(parsed);
        Assert.Equal(42L, (long)parsed["x"]);
        Assert.Equal(59L, (long)parsed["y"]);
        Assert.Equal(1L,  (long)parsed["level"]);
    }

    /// <summary>
    /// Cross-level location (level=2): the primary regression scenario.
    /// cf sends {"location":"{\"x\":10,\"y\":20,\"level\":2}"}.
    /// ParseJsonObjectArg must yield x=10,y=20,level=2.
    /// </summary>
    [Fact]
    public void WirePayload_CrossLevel_LocationStringParsesCorrectly()
    {
        var outerPayload = """{"location":"{\"x\":10,\"y\":20,\"level\":2}"}""";
        var outer = JsonNode.Parse(outerPayload) as JsonObject;

        var locationNode = outer["location"];
        Assert.IsNotType<JsonObject>(locationNode); // confirm: direct cast would fail

        var parsed = GoToHandlers.ParseJsonObjectArg(locationNode);

        Assert.NotNull(parsed);
        Assert.Equal(10L, (long)parsed["x"]);
        Assert.Equal(20L, (long)parsed["y"]);
        Assert.Equal(2L,  (long)parsed["level"]);
    }

    /// <summary>
    /// Verify that pre-fix behaviour (direct cast) fails for the wire format.
    /// This test documents WHY the fix was needed — the direct cast returns null
    /// for the string-encoded location that cf produces.
    /// </summary>
    [Fact]
    public void PreFixBehaviourDocumented_DirectCastReturnsNullForWireFormat()
    {
        // This is the bug: direct "as JsonObject" cast on a JsonValue node returns null.
        var outerPayload = """{"location":"{\"x\":10,\"y\":20,\"level\":2}"}""";
        var outer = JsonNode.Parse(outerPayload) as JsonObject;

        var locationNode = outer["location"];
        var directCast = locationNode as JsonObject; // pre-fix code

        // Document: this is null — the bug.
        Assert.Null(directCast);

        // But ParseJsonObjectArg returns a non-null result — the fix.
        var fixedResult = GoToHandlers.ParseJsonObjectArg(locationNode);
        Assert.NotNull(fixedResult);
    }

    /// <summary>
    /// Coordinate arithmetic: ParseJsonObjectArg result feeds the tile-to-subunit conversion.
    /// Verify x*16 and y*16 produce the expected wire coordinates (tile units → 1/16 units).
    /// </summary>
    [Fact]
    public void ParsedLocation_TileToSubunitConversion_ProducesCorrectWireCoords()
    {
        // cf sends location as string. Agent passes tile unit coords.
        var stringNode = JsonValue.Create("""{"x":42,"y":59,"level":2}""");
        var parsed = GoToHandlers.ParseJsonObjectArg(stringNode);

        Assert.NotNull(parsed);

        // Replicate the handler's conversion: tile units × 16 = subunit coords.
        var lx = (long)parsed["x"];
        var ly = (long)parsed["y"];
        var llv = (long?)parsed["level"] ?? 1L;

        var sx = (short)(lx * 16); // 42 * 16 = 672
        var sy = (short)(ly * 16); // 59 * 16 = 944
        var sl = (sbyte)llv;        // level 2

        Assert.Equal((short)672, sx);
        Assert.Equal((short)944, sy);
        Assert.Equal((sbyte)2,   sl);
    }
}
