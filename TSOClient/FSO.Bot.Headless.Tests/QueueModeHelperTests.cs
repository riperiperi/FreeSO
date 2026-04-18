using System.Text.Json.Nodes;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the queue-mode helper's pure surfaces: mode validation and
/// arg-default reading. The cancel-emission logic requires a live VMHost
/// (HeadlessVMHost.RunUnderTickLock + Driver.SendCommand against a real avatar's
/// Thread.Queue) and is covered by the integration test at
/// tests/integration/test_queue_mode_walk_breaks_spawn_idle.sh.
/// </summary>
public class QueueModeHelperTests
{
    [Fact]
    public void ReadQueueMode_AbsentArg_DefaultsToQueue()
    {
        var args = new JsonObject();
        Assert.Equal(QueueModeHelper.ModeQueue, QueueModeHelper.ReadQueueMode(args));
    }

    [Fact]
    public void ReadQueueMode_EmptyString_DefaultsToQueue()
    {
        var args = new JsonObject { ["queue_mode"] = "" };
        Assert.Equal(QueueModeHelper.ModeQueue, QueueModeHelper.ReadQueueMode(args));
    }

    [Theory]
    [InlineData("queue")]
    [InlineData("preempt")]
    public void ReadQueueMode_ValidValue_RoundTrips(string mode)
    {
        var args = new JsonObject { ["queue_mode"] = mode };
        Assert.Equal(mode, QueueModeHelper.ReadQueueMode(args));
    }

    [Fact]
    public void ReadQueueMode_NonStandardValue_PassesThroughForValidation()
    {
        // ApplyQueueMode is the gate, not ReadQueueMode — the latter is a parse-only helper.
        var args = new JsonObject { ["queue_mode"] = "yolo" };
        Assert.Equal("yolo", QueueModeHelper.ReadQueueMode(args));
    }

    [Fact]
    public void ApplyQueueMode_InvalidMode_ReturnsErrorWithoutHost()
    {
        // Invalid mode short-circuits before any VM access, so a null host is safe here.
        // (The VM access path is reached only after the mode passes validation.)
        var ok = QueueModeHelper.ApplyQueueMode(null, "yolo", out var cancelled, out var error);
        Assert.False(ok);
        Assert.Equal(0, cancelled);
        Assert.Contains("queue_mode must be", error);
        Assert.Contains("yolo", error);
    }
}
