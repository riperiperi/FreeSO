/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.Content;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Build-buy-architecture family command handlers (freesoexperiment-41d). Ten ops over
/// lot architecture: place-wall, paint-wall, paint-floor, paint-grass, flatten-terrain,
/// raise-terrain, set-roof, change-environment, change-lot-size, leave-build-buy. All
/// PDUs ride the <b>lot</b> socket via <see cref="HeadlessVMHost.Driver"/>
/// (VMClientDriver.SendCommand).
///
/// <para>
/// <b>PDU mapping</b>. Six ops share <c>VMNetArchitectureCmd</c> carrying a
/// <c>VMArchitectureCommand</c> list with a Type discriminator from
/// <c>VMArchitectureCommandType</c>:
///   - place-wall      → WALL_LINE  (or WALL_DELETE when "remove=true")
///   - paint-wall      → PATTERN_DOT / PATTERN_FILL
///   - paint-floor     → FLOOR_RECT / FLOOR_FILL
///   - paint-grass     → GRASS_DOT
///   - flatten-terrain → TERRAIN_FLATTEN
///   - raise-terrain   → TERRAIN_RAISE
/// The remaining four have dedicated PDUs:
///   - set-roof           → VMNetSetRoofCmd          (Pitch, Style)
///   - change-environment → VMNetChangeEnvironmentCmd (GUIDsToAdd, GUIDsToClear)
///   - change-lot-size    → VMNetChangeLotSizeCmd     (LotSize, LotStories)
///   - leave-build-buy    → VMNetLeaveBuildBuyCmd     (Build)
/// </para>
///
/// <para>
/// <b>Owner gating</b>. All architecture ops are owner-only per wave-2e spec (and per
/// real-world "you can't renovate someone else's house"). Every handler checks
/// <c>VMTSOLotState.OwnerID == MyAvatarPersistId</c> under RunUnderTickLock BEFORE the
/// PDU goes out — same deterministic refuse pattern as
/// <see cref="BuyModeHandlers.CheckLotOwner"/> / <see cref="PropertyHandlers.CheckOwner"/>.
/// Server-side Verify() also enforces build permissions (most arch cmds require
/// BuildBuyRoommate via <c>Validator.CanBuildTool</c>; change-lot-size and set-roof
/// require Owner); a local gate gives the agent a synchronous error it can see without
/// waiting for a server timeout (there is no negative-ACK — denial is a silent drop).
/// </para>
///
/// <para>
/// <b>leave-build-buy exception</b>. VMNetLeaveBuildBuyCmd.Verify always returns true.
/// The command is a "commit thumbnail" marker — the client fires it when closing the
/// build/buy panel. We still gate it on owner at the handler for consistency (the
/// agent shouldn't be calling leave-build-buy on lots it doesn't own).
/// </para>
///
/// <para>
/// <b>Thread safety</b>: every pre-PDU check reads VM state the tick thread mutates
/// (OwnerID). All reads route through <see cref="HeadlessVMHost.RunUnderTickLock{T}"/>;
/// outbound PDUs use Driver.SendCommand which is lock-protected per the wave-2a contract.
/// </para>
///
/// <para>
/// <b>query-architecture</b> is a test-support helper (not in the verb catalog). It reads
/// <c>vm.Context.Architecture</c> wall/floor/roof/size state at a given tile and returns a
/// snapshot the integration test can use for before/after baselines — perception does not
/// expose walls/floors. No PDU emission.
/// </para>
/// </summary>
public static class BuildModeHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("place-wall",        (args, ct) => Task.FromResult(PlaceWall(vmHost, args)));
        dispatcher.Register("paint-wall",        (args, ct) => Task.FromResult(PaintWall(vmHost, args)));
        dispatcher.Register("paint-floor",       (args, ct) => Task.FromResult(PaintFloor(vmHost, args)));
        dispatcher.Register("paint-grass",       (args, ct) => Task.FromResult(PaintGrass(vmHost, args)));
        dispatcher.Register("flatten-terrain",   (args, ct) => Task.FromResult(FlattenTerrain(vmHost, args)));
        dispatcher.Register("raise-terrain",     (args, ct) => Task.FromResult(RaiseTerrain(vmHost, args)));
        dispatcher.Register("set-roof",          (args, ct) => Task.FromResult(SetRoof(vmHost, args)));
        dispatcher.Register("change-environment",(args, ct) => Task.FromResult(ChangeEnvironment(vmHost, args)));
        dispatcher.Register("change-lot-size",   (args, ct) => Task.FromResult(ChangeLotSize(vmHost, args)));
        dispatcher.Register("leave-build-buy",   (args, ct) => Task.FromResult(LeaveBuildBuy(vmHost, args)));
        dispatcher.Register("query-architecture",        (args, ct) => Task.FromResult(QueryArchitecture(vmHost, args)));
        dispatcher.Register("list-architecture-styles", (args, ct) => Task.FromResult(ListArchitectureStyles(vmHost, args)));
    }

    // ---- owner gate ----

    internal static (bool isOwner, uint ownerId) CheckLotOwner(HeadlessVMHost vmHost)
    {
        return vmHost.RunUnderTickLock(() =>
        {
            var vm = vmHost.VM;
            if (vm == null) return (false, 0u);
            var lotState = vm.TSOState as VMTSOLotState;
            if (lotState == null) return (false, 0u);
            var ownerId = lotState.OwnerID;
            return (ownerId == vmHost.MyAvatarPersistId && ownerId != 0, ownerId);
        });
    }

    // ---- argument helpers ----

    private static long? L(JsonObject args, string key) => (long?)args[key];
    private static bool? B(JsonObject args, string key) => (bool?)args[key];
    private static double? D(JsonObject args, string key)
    {
        var n = args[key];
        if (n == null) return null;
        if (n is JsonValue v)
        {
            if (v.TryGetValue<double>(out var d)) return d;
            if (v.TryGetValue<long>(out var l)) return (double)l;
        }
        return null;
    }

    // ---- place-wall ----

    internal static CommandDispatcher.Response PlaceWall(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"place-wall: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("place-wall requires x, y (start tile coords, int)");
        var lengthArg = L(args, "length");
        var dirArg = L(args, "direction");
        if (!lengthArg.HasValue || !dirArg.HasValue)
            return CommandDispatcher.Response.Fail("place-wall requires length (tiles, >0) and direction (0..7 octant: 0=E, 2=S, 4=W, 6=N, odd=diagonals)");
        if (lengthArg.Value <= 0)
            return CommandDispatcher.Response.Fail("place-wall: length must be > 0");
        bool remove = B(args, "remove") ?? false;
        int pattern = (int)(L(args, "pattern") ?? 0);
        int style = (int)(L(args, "style") ?? 1); // style 1 = default wall style per UIWallPlacer

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = remove ? VMArchitectureCommandType.WALL_DELETE : VMArchitectureCommandType.WALL_LINE,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    x2 = (int)lengthArg.Value,
                    y2 = (int)dirArg.Value,
                    pattern = checked((ushort)pattern),
                    style = checked((ushort)style),
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true,
            verb = "place-wall",
            remove,
            x = (int)x.Value,
            y = (int)y.Value,
            length = (int)lengthArg.Value,
            direction = (int)dirArg.Value,
            pattern,
            style,
        });
    }

    // ---- paint-wall (PATTERN_DOT) ----

    internal static CommandDispatcher.Response PaintWall(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"paint-wall: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("paint-wall requires x, y (tile coords)");
        var side = L(args, "side");
        if (!side.HasValue)
            return CommandDispatcher.Response.Fail("paint-wall requires side (0..5; 0-3 for normal walls, 4-5 for diagonal sides — see VMArchitectureCommand.cs)");
        int pattern = (int)(L(args, "pattern") ?? 0);

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.PATTERN_DOT,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    x2 = (int)side.Value,
                    pattern = checked((ushort)pattern),
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "paint-wall",
            x = (int)x.Value, y = (int)y.Value, side = (int)side.Value, pattern,
        });
    }

    // ---- paint-floor (FLOOR_RECT) ----

    internal static CommandDispatcher.Response PaintFloor(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"paint-floor: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("paint-floor requires x, y (start tile coords)");
        // width/height default to 1×1 (single tile paint)
        int w = (int)(L(args, "width") ?? 1);
        int h = (int)(L(args, "height") ?? 1);
        if (w < 1 || h < 1)
            return CommandDispatcher.Response.Fail("paint-floor: width and height must be >= 1");
        int pattern = (int)(L(args, "pattern") ?? 0);
        bool fill = B(args, "fill") ?? false;

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = fill ? VMArchitectureCommandType.FLOOR_FILL : VMArchitectureCommandType.FLOOR_RECT,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    x2 = w,
                    y2 = h,
                    pattern = checked((ushort)pattern),
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "paint-floor",
            x = (int)x.Value, y = (int)y.Value, width = w, height = h, pattern, fill,
        });
    }

    // ---- paint-grass (GRASS_DOT) ----

    internal static CommandDispatcher.Response PaintGrass(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"paint-grass: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("paint-grass requires x, y (tile coords)");
        // "pattern" acts as grass density/type (0..255 packed in ushort).
        int pattern = (int)(L(args, "pattern") ?? 0);

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.GRASS_DOT,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    pattern = checked((ushort)pattern),
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "paint-grass",
            x = (int)x.Value, y = (int)y.Value, pattern,
        });
    }

    // ---- flatten-terrain ----

    internal static CommandDispatcher.Response FlattenTerrain(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"flatten-terrain: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("flatten-terrain requires x, y (tile coords)");
        int w = (int)(L(args, "width") ?? 1);
        int h = (int)(L(args, "height") ?? 1);
        if (w < 1 || h < 1)
            return CommandDispatcher.Response.Fail("flatten-terrain: width and height must be >= 1");

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.TERRAIN_FLATTEN,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    x2 = w,
                    y2 = h,
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "flatten-terrain",
            x = (int)x.Value, y = (int)y.Value, width = w, height = h,
        });
    }

    // ---- raise-terrain ----

    internal static CommandDispatcher.Response RaiseTerrain(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"raise-terrain: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("raise-terrain requires x, y (tile coords)");
        // delta stored in x2: positive raises, negative lowers
        int delta = (int)(L(args, "delta") ?? 1);
        if (delta == 0)
            return CommandDispatcher.Response.Fail("raise-terrain: delta must be non-zero (positive raises, negative lowers)");

        var cmd = new VMNetArchitectureCmd
        {
            Commands = new List<VMArchitectureCommand>
            {
                new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.TERRAIN_RAISE,
                    level = (sbyte)(L(args, "level") ?? 1),
                    x = (int)x.Value,
                    y = (int)y.Value,
                    x2 = delta,
                }
            }
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "raise-terrain",
            x = (int)x.Value, y = (int)y.Value, delta,
        });
    }

    // ---- set-roof ----

    internal static CommandDispatcher.Response SetRoof(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"set-roof: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var styleArg = L(args, "style");
        if (!styleArg.HasValue || styleArg.Value < 0)
            return CommandDispatcher.Response.Fail("set-roof requires style (uint index into Content.WorldRoofs)");
        // Pitch: float clamped server-side to [0, 1.25]. Default 0.5 (the UIRoofer stock default).
        double pitch = D(args, "pitch") ?? 0.5;
        if (pitch < 0.0) pitch = 0.0;
        if (pitch > 1.25) pitch = 1.25;

        var cmd = new VMNetSetRoofCmd
        {
            Style = checked((uint)styleArg.Value),
            Pitch = (float)pitch,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "set-roof",
            style = (long)cmd.Style,
            pitch = (double)cmd.Pitch,
        });
    }

    // ---- change-environment ----

    internal static CommandDispatcher.Response ChangeEnvironment(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"change-environment: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var add = ParseGuidArray(args["guids_to_add"]);
        var clear = ParseGuidArray(args["guids_to_clear"]);
        if (add.Count == 0 && clear.Count == 0)
            return CommandDispatcher.Response.Fail("change-environment requires at least one of guids_to_add or guids_to_clear (uint arrays)");
        // Server caps each list at 40 (VMNetChangeEnvironmentCmd.Deserialize).
        if (add.Count > 40 || clear.Count > 40)
            return CommandDispatcher.Response.Fail("change-environment: guids_to_add and guids_to_clear are each capped at 40 entries (server side)");

        var cmd = new VMNetChangeEnvironmentCmd
        {
            GUIDsToAdd = add,
            GUIDsToClear = clear,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "change-environment",
            guids_to_add = add.Count,
            guids_to_clear = clear.Count,
        });
    }

    private static List<uint> ParseGuidArray(JsonNode n)
    {
        var list = new List<uint>();
        if (n is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is JsonValue v && v.TryGetValue<long>(out var g))
                {
                    list.Add(checked((uint)g));
                }
            }
        }
        return list;
    }

    // ---- change-lot-size ----

    internal static CommandDispatcher.Response ChangeLotSize(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"change-lot-size: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        var sizeArg = L(args, "lot_size");
        if (!sizeArg.HasValue || sizeArg.Value < 0 || sizeArg.Value > 255)
            return CommandDispatcher.Response.Fail("change-lot-size requires lot_size (byte index into VMBuildableAreaInfo.BuildableSizes)");
        var storiesArg = L(args, "lot_stories");
        if (!storiesArg.HasValue || storiesArg.Value < 0 || storiesArg.Value > 3)
            return CommandDispatcher.Response.Fail("change-lot-size requires lot_stories (byte, 0..3)");

        var cmd = new VMNetChangeLotSizeCmd
        {
            LotSize = checked((byte)sizeArg.Value),
            LotStories = checked((byte)storiesArg.Value),
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "change-lot-size",
            lot_size = (int)cmd.LotSize,
            lot_stories = (int)cmd.LotStories,
        });
    }

    // ---- leave-build-buy ----

    internal static CommandDispatcher.Response LeaveBuildBuy(HeadlessVMHost vmHost, JsonObject args)
    {
        var (isOwner, ownerId) = CheckLotOwner(vmHost);
        if (!isOwner)
            return CommandDispatcher.Response.Fail($"leave-build-buy: caller is not lot owner (owner_id={ownerId}, me={vmHost.MyAvatarPersistId})");

        // Build=true means "leaving build mode" (vs buy mode). Default true since architecture
        // family is build-mode-centric; agents can override.
        bool build = B(args, "build") ?? true;

        var cmd = new VMNetLeaveBuildBuyCmd
        {
            Build = build,
        };
        vmHost.Driver.SendCommand(cmd);
        return CommandDispatcher.Response.Success(new
        {
            queued = true, verb = "leave-build-buy", build,
        });
    }

    // ---- query-architecture (test helper; NOT a verb-catalog op) ----

    /// <summary>
    /// Read-only snapshot of the lot's architecture state at a tile (or roof + size globally).
    /// Used by the integration test to establish baselines and verify wall/floor changes
    /// stuck — perception's lot block does not expose walls/floors. No PDU emission.
    /// Args: x (tile), y (tile), level (default 1).
    /// Returns: wall{top_left_pattern, top_left_style, top_right_pattern, top_right_style, segments},
    ///          floor{pattern}, roof{pitch, style}, lot{size, stories}.
    /// </summary>
    internal static CommandDispatcher.Response QueryArchitecture(HeadlessVMHost vmHost, JsonObject args)
    {
        var x = L(args, "x"); var y = L(args, "y");
        if (!x.HasValue || !y.HasValue)
            return CommandDispatcher.Response.Fail("query-architecture requires x, y (tile coords)");
        int level = (int)(L(args, "level") ?? 1);

        var payload = vmHost.RunUnderTickLock<object>(() =>
        {
            var vm = vmHost.VM;
            if (vm?.Context?.Architecture == null) return null;
            var arch = vm.Context.Architecture;
            var wall = arch.GetWall((short)x.Value, (short)y.Value, (sbyte)level);
            var floor = arch.GetFloor((short)x.Value, (short)y.Value, (sbyte)level);
            var lotState = vm.TSOState as VMTSOLotState;
            int sizeRaw = lotState?.Size ?? 0;
            return new
            {
                x = (int)x.Value,
                y = (int)y.Value,
                level,
                wall = new
                {
                    top_left_pattern  = (int)wall.TopLeftPattern,
                    top_left_style    = (int)wall.TopLeftStyle,
                    top_right_pattern = (int)wall.TopRightPattern,
                    top_right_style   = (int)wall.TopRightStyle,
                    segments          = (int)wall.Segments,
                },
                floor = new
                {
                    pattern = (int)floor.Pattern,
                },
                roof = new
                {
                    pitch = (double)arch.RoofPitch,
                    style = (long)arch.RoofStyle,
                },
                lot = new
                {
                    size_tiles = sizeRaw & 0xFF,
                    stories    = (sizeRaw >> 8) & 0xFF,
                },
            };
        });

        if (payload == null) return CommandDispatcher.Response.Fail("query-architecture: no live VM");
        return CommandDispatcher.Response.Success(payload);
    }

    // ---- list-architecture-styles (freesoexperiment-8a8) ----

    /// <summary>
    /// Read-only catalog of wall patterns, wall styles, and floor patterns the agent can
    /// reference when calling paint-wall, place-wall, and paint-floor.
    ///
    /// <para>
    /// Three sub-lists are returned, each with typed pricing units:
    ///   - <b>wall_patterns</b>: textures applied via paint-wall. Source: WorldWallProvider.Entries
    ///     (WallReference dictionary). Price is per wall segment.
    ///   - <b>wall_styles</b>: the 5 structural variants for place-wall (wall, picket fence, iron
    ///     fence, privacy fence, banisters). Source: WorldWallProvider.WallStyleIDs[] →
    ///     GetWallStyle(id). Price is per segment; is_door marks dynamic door styles.
    ///   - <b>floor_patterns</b>: textures applied via paint-floor. Source: WorldFloorProvider.Entries
    ///     (FloorReference dictionary). Price is per tile.
    /// </para>
    ///
    /// <para>
    /// Roof styles are intentionally excluded — VMArchitecture.RoofStyle is a bare uint with no
    /// label catalog. Use set-roof with a numeric index (query-architecture returns current state).
    /// No PDU emission; no VM lock needed (Content is immutable after Init).
    /// </para>
    /// </summary>
    internal static CommandDispatcher.Response ListArchitectureStyles(HeadlessVMHost vmHost, JsonObject args)
    {
        try
        {
            var content = Content.Content.Get();
            var walls = content.WorldWalls;
            var floors = content.WorldFloors;

            if (walls == null || floors == null)
                return CommandDispatcher.Response.Fail("list-architecture-styles: content providers not initialised");

            // Wall patterns: textures applied via paint-wall (one per wall face side).
            var wallPatterns = walls.Entries.Values
                .Select(e => (object)new
                {
                    id              = (long)e.ID,
                    name            = e.Name ?? "",
                    price_per_segment = (long)e.Price,
                    description     = e.Description ?? "",
                })
                .ToList();

            // Wall styles: the 5 structural variants (wall / picket fence / iron fence /
            // privacy fence / banisters). WallStyleIDs is the authoritative source of the 5.
            var wallStyles = walls.WallStyleIDs
                .Select(id =>
                {
                    var style = walls.GetWallStyle(id);
                    if (style == null) return null;
                    return (object)new
                    {
                        id              = (long)style.ID,
                        name            = style.Name ?? "",
                        price_per_segment = (long)style.Price,
                        is_door         = style.IsDoor,
                    };
                })
                .Where(s => s != null)
                .ToList();

            // Floor patterns: textures applied via paint-floor (one per tile).
            var floorPatterns = floors.Entries.Values
                .Select(e => (object)new
                {
                    id             = (long)e.ID,
                    name           = e.Name ?? "",
                    price_per_tile = (long)e.Price,
                    description    = e.Description ?? "",
                })
                .ToList();

            return CommandDispatcher.Response.Success(new
            {
                wall_patterns  = wallPatterns,
                wall_styles    = wallStyles,
                floor_patterns = floorPatterns,
            });
        }
        catch (Exception ex)
        {
            return CommandDispatcher.Response.Fail($"list-architecture-styles: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
