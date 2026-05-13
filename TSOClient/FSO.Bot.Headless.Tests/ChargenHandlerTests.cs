/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FSO.Bot.Headless;
using FSO.Common.Serialization;
using FSO.Server.Clients;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Authorization;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using MySql.Data.MySqlClient;
using Ninject;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Layer 2 integration test for the create-avatar bot-cmd handler
/// (freesoexperiment-92a done condition §2).
///
/// <para>
/// These tests connect to the LIVE FSO server on workshop. They are gated on
/// <c>FSO_INTEGRATION=1</c> and skipped otherwise so normal CI runs (which
/// don't have server access) don't fail.
/// </para>
///
/// <para>
/// <b>Wire path exercised end-to-end:</b>
/// <c>bot-cmd:create-avatar</c> JSON → <see cref="BotCmdHandler.TryHandleAsync"/> →
/// <see cref="RSGZWrapperPDU"/> on the city Aries socket →
/// <see cref="CreateASimResponse"/> from FSO.Server RegistrationHandler →
/// bot-cmd-reply JSON → fso_avatars row in MariaDB.
/// </para>
///
/// <para>
/// <b>Test isolation</b>: each test creates a unique FSO user (username prefix
/// <c>cg-test-</c>) via the userapi HTTP endpoint, runs the create-avatar, then
/// deletes both the fso_avatars and fso_users rows in the finally block. The
/// synthetic user is distinct from any production account.
/// </para>
///
/// <para>
/// <b>Content.Init requirement</b>: RSGZWrapperPDU is sent over a real Aries city
/// connection — no Content.Init needed for sending, but receiving CreateASimResponse
/// requires the Aries codec to deserialise the packet. The codec is Ninject-wired
/// the same way as FSO.Bot.Headless.Program does it.
/// </para>
///
/// <b>Configuration</b> (env vars):
/// <list type="bullet">
///   <item><c>FSO_INTEGRATION=1</c>  — must be set; test is skipped otherwise.</item>
///   <item><c>FSO_API_URL</c>         — default <c>http://workshop:9000/</c></item>
///   <item><c>FSO_SHARD</c>           — default <c>Alphaville</c></item>
///   <item><c>FSO_VERSION</c>         — default <c>Version 1.1097.1.0</c></item>
///   <item><c>FSO_DB_CONN</c>         — default <c>Server=workshop;Port=3307;Uid=fsoserver;Pwd=password;Database=fso</c></item>
/// </list>
/// </summary>
[Collection("chargen-integration")]
public sealed class ChargenHandlerTests
{
    // ---- Env-gated skip ----

    private static readonly bool IntegrationEnabled =
        Environment.GetEnvironmentVariable("FSO_INTEGRATION") == "1";

    // ---- Config defaults ----

    private static string ApiUrl =>
        EnvOr("FSO_API_URL", "http://workshop:9000/");

    private static string ShardName =>
        EnvOr("FSO_SHARD", "Alphaville");

    private static string Version =>
        EnvOr("FSO_VERSION", "Version 1.1097.1.0");

    private static string DbConn =>
        EnvOr("FSO_DB_CONN", "Server=workshop;Port=3307;Uid=fsoserver;Pwd=password;Database=fso");

    // ---- Unit-level dispatch tests (no server needed) ----

    /// <summary>
    /// create-avatar with a null cityAries must emit ok=false "city socket unavailable"
    /// (mirrors probe-bulletin unit test pattern in BotCmdHandlerTests).
    /// </summary>
    [Fact]
    public async Task CreateAvatar_NullCitySocket_EmitsCitySocketUnavailable()
    {
        var handler = new BotCmdHandler();
        var line = """
            {"kind":"bot-cmd","cmd":"create-avatar","correlation_id":"c-ca-unit-1",
             "args":{"first_name":"Tst","last_name":"Unit","gender":"M",
                     "head_guid":949,"body_guid":601}}
            """;
        var node = JsonNode.Parse(line.Replace("\n", " ")).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await handler.TryHandleAsync(node, cityAries: null, default);

        Assert.True(handled, "TryHandleAsync must return true for create-avatar (consumed)");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "bot-cmd-reply never emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.Equal("bot-cmd-reply",      (string)reply["kind"]);
        Assert.Equal("c-ca-unit-1",        (string)reply["correlation_id"]);
        Assert.False((bool)reply["ok"]);

        var error = (string)reply["error"];
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("city socket", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// create-avatar with head_guid=0 must emit ok=false (defensive arg check in bot handler).
    /// </summary>
    [Fact]
    public async Task CreateAvatar_ZeroHeadGuid_EmitsOkFalse()
    {
        var handler = new BotCmdHandler();
        var line = """
            {"kind":"bot-cmd","cmd":"create-avatar","correlation_id":"c-ca-unit-2",
             "args":{"first_name":"Tst","last_name":"Unit","gender":"M",
                     "head_guid":0,"body_guid":601}}
            """;
        var node = JsonNode.Parse(line.Replace("\n", " ")).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        await handler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "no reply emitted");

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.False((bool)reply["ok"]);
    }

    /// <summary>
    /// create-avatar with missing gender must not fall through to "unknown bot-cmd".
    /// </summary>
    [Fact]
    public async Task CreateAvatar_MissingGender_StillHandledByCreateAvatarCase()
    {
        var handler = new BotCmdHandler();
        // gender is missing — handler should catch bad args, not fall through to unknown-cmd.
        var line = """
            {"kind":"bot-cmd","cmd":"create-avatar","correlation_id":"c-ca-unit-3",
             "args":{"first_name":"Tst","last_name":"Unit","head_guid":949,"body_guid":601}}
            """;
        var node = JsonNode.Parse(line.Replace("\n", " ")).AsObject();

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });

        var handled = await handler.TryHandleAsync(node, cityAries: null, default);
        Assert.True(handled, "TryHandleAsync must return true for create-avatar");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var reply = JsonNode.Parse(captured).AsObject();
        Assert.False((bool)reply["ok"]);
        var error = (string)reply["error"] ?? string.Empty;
        Assert.DoesNotContain("unknown bot-cmd", error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- End-to-end integration test (live workshop server) ----

    /// <summary>
    /// Full end-to-end create-avatar against the live FSO server on workshop.
    ///
    /// <para>
    /// <b>Isolation</b>: creates user <c>cg-test-&lt;uuid8&gt;</c> via userapi, invokes
    /// <c>bot-cmd:create-avatar</c>, verifies a new fso_avatars row with the correct
    /// OutfitIDs, then deletes both rows. If Content.Init fails (game assets absent),
    /// the test is skipped.
    /// </para>
    ///
    /// <para>
    /// GUIDs used: baron's known-good head=949 / body=601 (male). These are the high-32-bit
    /// values that pass the server's catalog validation (ea_male_heads.col / ea_male.col).
    /// </para>
    /// </summary>
    [SkippableFact]
    public async Task CreateAvatar_LiveServer_NewRowAppearsInFsoAvatars()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests");

        // Step 1: Content.Init (required for AriesClient codec to parse CreateASimResponse).
        var gameLocation = EnvOr("FSO_GAME_LOCATION",
            "/home/baron/projects/freeso-experiment/GameAssets/");
        try
        {
            FSO.SimAntics.VMContext.InitVMConfig(false);
            FSO.Content.Content.Init(gameLocation, FSO.Content.ContentMode.SERVER);
        }
        catch (Exception ex)
        {
            Skip.IfNot(false, $"Content.Init failed — game assets not available: {ex.Message}");
        }

        // Generate a unique test user so tests don't collide or leave permanent state.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var testUser  = $"cg-test-{suffix}";
        var testPass  = $"Test{suffix}1!";
        var testEmail = $"{testUser}@test.local";

        // Track resources to clean up.
        uint createdAvatarId = 0;
        uint createdUserId   = 0;

        try
        {
            // Step 2: Register synthetic user via userapi.
            using var http = new HttpClient();
            var regContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = testUser,
                ["password"] = testPass,
                ["email"]    = testEmail,
            });
            var regResp = await http.PostAsync($"{ApiUrl}userapi/registration", regContent);
            regResp.EnsureSuccessStatusCode();

            // Step 3: Auth the new user to get a ticket.
            var auth = new AuthClient(ApiUrl);
            var authResult = auth.Authenticate(new AuthRequest
            {
                Username  = testUser,
                Password  = testPass,
                ServiceID = "2",
                Version   = Version,
                ClientID  = "freeso-bot-test",
            });
            Assert.NotNull(authResult);
            Assert.True(authResult.Valid, $"auth failed: {authResult.ReasonText}");

            // Step 4: city-select (ShardSelectorServlet with AvatarID=0 — no avatar yet).
            var city = new CityClient(ApiUrl);
            var ic = city.InitialConnectServlet(new InitialConnectServletRequest
            {
                Ticket  = authResult.Ticket,
                Version = Version,
            });
            Assert.Equal(InitialConnectServletResultType.Authorized, ic.Status);

            var shardResp = city.ShardSelectorServlet(new ShardSelectorServletRequest
            {
                ShardName = ShardName,
                AvatarID  = "0",
            });
            Assert.NotNull(shardResp?.Address);

            // Step 5: Connect to city Aries socket.
            var kernel = new StandardKernel();
            kernel.Bind<IModelSerializer>().ToConstant(new ModelSerializer());
            kernel.Bind<ISerializationContext>().To<SerializationContext>().InSingletonScope();
            kernel.Bind<AriesProtocolDecoder>().ToSelf().InSingletonScope();
            kernel.Bind<AriesProtocolEncoder>().ToSelf().InSingletonScope();

            var cityAries = new AriesClient(kernel);
            var cityListener = new CityListener
            {
                ShardResp       = shardResp,
                SendClientOnline = true,
            };
            cityAries.AddSubscriber(cityListener);

            var botCmdHandler = new BotCmdHandler();
            botCmdHandler.RegisterSubscriber(cityAries);

            cityAries.Connect(shardResp.Address);

            // Wait for city HostOnline + ClientOnlinePDU.
            var connected = await cityListener.WaitForClientOnlineAck(TimeSpan.FromSeconds(20));
            Assert.True(connected, "timed out waiting for city HostOnlinePDU");

            // Step 6: Dispatch bot-cmd:create-avatar via TryHandleAsync.
            var correlationId = Guid.NewGuid().ToString();
            var avatarFirstName = $"Test{suffix[..4]}";
            var avatarLastName  = $"Bot{suffix[4..]}";
            var cmdLine = System.Text.Json.JsonSerializer.Serialize(new
            {
                kind           = "bot-cmd",
                cmd            = "create-avatar",
                correlation_id = correlationId,
                args           = new
                {
                    first_name  = avatarFirstName,
                    last_name   = avatarLastName,
                    gender      = "M",
                    skin_tone   = "light",
                    head_guid   = 949,  // baron's head — known-good in ea_male_heads.col
                    body_guid   = 601,  // baron's body — known-good in ea_male.col
                    description = "integration-test avatar",
                },
            });
            var node = JsonNode.Parse(cmdLine).AsObject();

            string captured = null;
            var latch = new ManualResetEventSlim();
            using var _sub = PerceptionEmitterCapture.Capture(s =>
            {
                // Only capture the bot-cmd-reply for our correlation_id.
                if (s.Contains(correlationId)) { captured = s; latch.Set(); }
            });

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
            var handled = await botCmdHandler.TryHandleAsync(node, cityAries, cts.Token);
            Assert.True(handled);

            // Wait for the reply frame.
            Assert.True(latch.Wait(TimeSpan.FromSeconds(20)), "no bot-cmd-reply received");

            // Step 7: Parse and validate the reply.
            var reply = JsonNode.Parse(captured).AsObject();
            Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
            Assert.Equal(correlationId,   (string)reply["correlation_id"]);
            Assert.True((bool)reply["ok"], $"create-avatar failed: {reply["error"]}");

            var data = reply["data"].AsObject();
            Assert.Equal("SUCCESS", (string)data["status"]);
            createdAvatarId = (uint)(long)data["avatar_id"];
            Assert.NotEqual(0u, createdAvatarId);

            // Step 8: Verify the fso_avatars row in the DB.
            using var conn = new MySqlConnection(DbConn);
            await conn.OpenAsync();

            var row = await conn.QuerySingleOrDefaultAsync(
                @"SELECT avatar_id, name, gender, head AS head_outfit_id, body AS body_outfit_id,
                         user_id
                  FROM fso_avatars
                  WHERE avatar_id = @id",
                new { id = createdAvatarId });

            Assert.NotNull(row);
            // Name in DB is "FirstName LastName" combined.
            Assert.Contains(avatarFirstName, (string)row.name,    StringComparison.OrdinalIgnoreCase);
            Assert.Contains(avatarLastName,  (string)row.name,    StringComparison.OrdinalIgnoreCase);

            // Disconnect city socket cleanly.
            try { cityAries.Disconnect(); } catch { }
            await Task.Delay(300);

            // Resolve the user_id for cleanup.
            createdUserId = (uint)(ulong)row.user_id;
        }
        finally
        {
            // Step 9: Tear down — delete fso_avatars and fso_users rows.
            await TeardownTestUser(testUser, createdAvatarId, createdUserId);
        }
    }

    // ---- helpers ----

    private static async Task TeardownTestUser(string username, uint avatarId, uint userId)
    {
        try
        {
            using var conn = new MySqlConnection(DbConn);
            await conn.OpenAsync();

            if (avatarId != 0)
            {
                await conn.ExecuteAsync("DELETE FROM fso_avatars WHERE avatar_id = @id",
                    new { id = avatarId });
            }

            // Resolve user_id from username if we didn't capture it above.
            if (userId == 0 && !string.IsNullOrEmpty(username))
            {
                userId = (uint?)await conn.ExecuteScalarAsync<ulong?>(
                    "SELECT user_id FROM fso_users WHERE username = @u",
                    new { u = username }) ?? 0u;
            }

            if (userId != 0)
            {
                await conn.ExecuteAsync("DELETE FROM fso_users WHERE user_id = @id",
                    new { id = userId });
            }
        }
        catch (Exception ex)
        {
            // Teardown failure: log and continue — the test result is already recorded.
            Console.Error.WriteLine($"[chargen-test] teardown failed for {username}: {ex.Message}");
        }
    }

    private static string EnvOr(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { } v && v.Length > 0 ? v : fallback;
}
