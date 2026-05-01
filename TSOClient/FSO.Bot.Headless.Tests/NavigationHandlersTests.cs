/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for NavigationHandlers.ReadHomeLotLocation and the no-owned-lot
/// path in GoHome (freesoexperiment-084).
///
/// HeadlessVMHost requires Content.Init which is unavailable in unit tests —
/// the VM-touching paths (already_home check, deferred marker) are covered by
/// the integration harness. These tests cover the pure-logic paths:
///
///   1. ReadHomeLotLocation reads FSO_HOME_LOT_LOCATION (decimal and hex).
///   2. ReadHomeLotLocation falls back to _fallbackHomeLotLocation when env absent/zero.
///   3. GoHome returns ok:false early (before touching the VM) when no owned lot.
/// </summary>
public class NavigationHandlersTests : IDisposable
{
    // ---------- env-isolation helpers ----------

    // Each test must not pollute the process environment. We save/restore
    // FSO_HOME_LOT_LOCATION around each test via the IDisposable pattern.
    private readonly string _priorEnv;

    public NavigationHandlersTests()
    {
        _priorEnv = Environment.GetEnvironmentVariable("FSO_HOME_LOT_LOCATION");
        // Clear before each test; individual tests set what they need.
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", null);
    }

    public void Dispose()
    {
        // Restore prior value (null means unset).
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", _priorEnv);
    }

    // ---------- ReadHomeLotLocation — decimal form ----------

    [Fact]
    public void ReadHomeLotLocation_DecimalEnv_ReturnsCorrectValue()
    {
        // 16318812 = 0xF9015C — baron's lot location.
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", "16318812");

        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(16318812u, got);
    }

    // ---------- ReadHomeLotLocation — hex "0x..." form ----------

    [Fact]
    public void ReadHomeLotLocation_HexEnv_ReturnsCorrectValue()
    {
        // OwnedLotEntry.LocationHex is a "0x..." hex string.
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", "0xF9015C");

        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(0xF9015Cu, got);
    }

    // ---------- ReadHomeLotLocation — absent / zero env → fallback ----------

    [Fact]
    public void ReadHomeLotLocation_EnvAbsent_UsesFallback()
    {
        // Env is cleared in constructor. Force fallback by calling RegisterAll
        // with a known homeLotLocation via the static field path.
        // We test the fallback indirectly: env absent + _fallbackHomeLotLocation=0 → 0.
        // (Zero triggers the no-owned-lot path in GoHome.)
        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(0u, got);
    }

    [Fact]
    public void ReadHomeLotLocation_ZeroEnv_UsesFallback()
    {
        // Explicit zero in env — treated the same as absent (parse succeeds but
        // the result is 0, which falls through to the fallback).
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", "0");

        // With _fallbackHomeLotLocation == 0 (never set in this test), expect 0.
        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(0u, got);
    }

    [Fact]
    public void ReadHomeLotLocation_InvalidEnv_UsesFallback()
    {
        // Garbage in env must not throw — fall through to fallback silently.
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", "not-a-number");

        // _fallbackHomeLotLocation == 0 (never set) → expect 0, no exception.
        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(0u, got);
    }

    // ---------- GoHome — no-owned-lot path (ok:false, no VM touch) ----------

    [Fact]
    public void GoHome_NoOwnedLot_ReturnsOkFalseWithoutTouchingVM()
    {
        // FSO_HOME_LOT_LOCATION absent, _fallbackHomeLotLocation not set → 0.
        // GoHome should return ok:false immediately, before calling vmHost.
        // Passing null for vmHost proves it doesn't dereference it.

        var response = NavigationHandlers.GoHome(null);

        Assert.False(response.Ok, $"expected ok:false for no-owned-lot, got ok:{response.Ok} payload:{response.Payload}");
        // Error message should mention "no owned lot" or "not purchased".
        Assert.NotNull(response.Error);
        Assert.Contains("owned lot", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Integration-oriented: env set → ReadHomeLotLocation picks it up ----------

    [Fact]
    public void ReadHomeLotLocation_HexWithUpperCasePrefix_Parses()
    {
        // "0XF9015C" — uppercase 0X prefix must be accepted.
        Environment.SetEnvironmentVariable("FSO_HOME_LOT_LOCATION", "0XF9015C");

        uint got = NavigationHandlers.ReadHomeLotLocation();
        Assert.Equal(0xF9015Cu, got);
    }
}
