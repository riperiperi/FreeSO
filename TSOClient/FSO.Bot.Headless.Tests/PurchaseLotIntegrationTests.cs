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
/// Real-server regression test for the purchase-lot NRE root cause
/// (automataisland-e49 / automataisland-67c veracity rework).
///
/// <para>
/// <b>Root cause:</b> <c>PurchaseLotHandler.cs</c> line 70 —
/// <c>db.Neighborhoods.GetByLocation()</c> returns <c>null</c> when
/// <c>fso_neighborhoods</c> is empty (e.g. a fresh grid31337-test sandbox).
/// Accessing <c>targNhood.neighborhood_id</c> on <c>null</c> threw
/// <c>NullReferenceException</c>, which the server's generic catch turned into
/// an untyped error string.
/// </para>
///
/// <para>
/// <b>Fix:</b> null guard added in <c>PurchaseLotHandler.cs</c> — when
/// <c>targNhood == null</c> the handler writes <c>FAILED/LOT_NOT_PURCHASABLE</c>
/// instead of crashing.
/// </para>
///
/// <para>
/// <b>Regression coverage (this file):</b> exercises the real code path:
/// <c>bot-cmd:purchase-lot</c> JSON →
/// <see cref="BotCmdHandler.TryHandleAsync"/> →
/// <c>PurchaseLotRequest</c> on real Aries city socket (grid31337-test, localhost:33300) →
/// <c>PurchaseLotHandler.cs</c> at the live FSO City server →
/// <c>db.Neighborhoods.GetByLocation()</c> against real MariaDB (fso_neighborhoods
/// temporarily emptied to reproduce the NRE-triggering condition) →
/// <c>PurchaseLotResponse</c> back to the bot →
/// bot-cmd-reply emitted via <c>PerceptionEmitter</c>.
///
/// The test asserts the reply is a TYPED <c>FAILED/LOT_NOT_PURCHASABLE</c> response
/// (not a <c>NullReferenceException</c> string), proving the null guard in
/// <c>PurchaseLotHandler.cs</c> fires correctly on the real live path.
/// </para>
///
/// <para>
/// <b>Gate:</b> <c>FSO_INTEGRATION=1</c> — skipped in CI unless the grid31337-test
/// Docker stack is running and accessible on this machine.
/// </para>
///
/// <para>
/// <b>Infrastructure required:</b>
/// <list type="bullet">
///   <item>MariaDB accessible at <c>FSO_DB_CONN_TEST</c>
///     (default: <c>Server=127.0.0.1;Port=3308;Uid=fsoserver;Pwd=password;Database=fso;SslMode=None</c>).
///   </item>
///   <item>FSO City server accessible on port 33300 (city Aries socket).</item>
///   <item>FSO userapi on <c>http://localhost:9200/</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>DB isolation:</b> The test creates a synthetic user and avatar (prefix
/// <c>plint</c>), temporarily deletes all rows from <c>fso_neighborhoods</c>,
/// runs the assertion, then restores the neighborhood row and deletes the test
/// user/avatar in a <c>finally</c> block.
/// </para>
///
/// <para>
/// <b>Configuration</b> (env vars):
/// <list type="bullet">
///   <item><c>FSO_INTEGRATION=1</c>  — must be set; test is skipped otherwise.</item>
///   <item><c>FSO_API_URL</c>        — default <c>http://localhost:9200/</c></item>
///   <item><c>FSO_SHARD</c>          — default <c>Alphaville</c></item>
///   <item><c>FSO_DB_CONN_TEST</c>   — default <c>Server=127.0.0.1;Port=3308;Uid=fsoserver;Pwd=password;Database=fso;SslMode=None</c></item>
/// </list>
/// </para>
/// </summary>
[Collection("purchase-lot-integration")]
public sealed class PurchaseLotIntegrationTests
{
    // ---- Env-gated skip ----

    private static readonly bool IntegrationEnabled =
        Environment.GetEnvironmentVariable("FSO_INTEGRATION") == "1";

    // ---- Config defaults ----

    private static string ApiUrl =>
        EnvOr("FSO_API_URL", "http://localhost:9200/");

    private static string ShardName =>
        EnvOr("FSO_SHARD", "Alphaville");

    /// <summary>
    /// Connection string for the test MariaDB instance.
    /// <para>
    /// Override with <c>FSO_DB_CONN_TEST</c>. Default targets the grid31337-test
    /// container port (3308) on 127.0.0.1 — accessible from both the host and from
    /// Docker containers running with <c>--network=host</c>.
    /// </para>
    /// </summary>
    private static string DbConnTest =>
        EnvOr("FSO_DB_CONN_TEST",
            "Server=127.0.0.1;Port=3308;Uid=fsoserver;Pwd=password;Database=fso;SslMode=None");

    // ---- Main regression test ----

    /// <summary>
    /// Regression test: purchase-lot against real FSO city server with
    /// <c>fso_neighborhoods</c> temporarily emptied.
    ///
    /// <para>
    /// <b>Pre-fix behaviour:</b> <c>PurchaseLotHandler.cs</c> called
    /// <c>targNhood.neighborhood_id</c> on the null returned by
    /// <c>db.Neighborhoods.GetByLocation()</c> → <c>NullReferenceException</c> →
    /// the bot received a generic error string containing "NullReferenceException".
    /// </para>
    ///
    /// <para>
    /// <b>Post-fix behaviour:</b> null guard fires → server writes
    /// <c>PurchaseLotResponse{Status=FAILED, Reason=LOT_NOT_PURCHASABLE}</c> →
    /// bot-cmd-reply has <c>ok=true, status="FAILED", reason="LOT_NOT_PURCHASABLE"</c>
    /// (C# layer contract: ok=true so Go sidecar can inspect typed status/reason).
    /// </para>
    ///
    /// <para>
    /// <b>Done condition:</b> the assertion checks BOTH that we do NOT receive an NRE string
    /// AND that we DO receive the typed FAILED/LOT_NOT_PURCHASABLE reason.
    /// </para>
    /// </summary>
    [SkippableFact]
    public async Task PurchaseLot_EmptyNeighborhoodsTable_TypedFailureNotNRE()
    {
        Skip.IfNot(IntegrationEnabled,
            "set FSO_INTEGRATION=1 to run live-server integration tests (requires grid31337-test stack)");

        // Generate a unique test user to avoid collision.
        // Username regex: ^([a-z0-9]){1}([a-z0-9_]){2,23}$ — no hyphens.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var testUser  = $"plint{suffix}";   // "plint" prefix = purchase-lot integration test
        var testPass  = $"t3st{suffix}";
        var testEmail = $"{testUser}@test.local";

        // Track for cleanup.
        uint createdUserId   = 0;
        uint createdAvatarId = 0;

        // Track the saved neighborhood rows for restoration.
        List<DbNeighborhoodSnapshot> savedNeighborhoods = null;
        bool neighborhoodDeleted = false;

        try
        {
            // ---- Step 1: register test user via userapi ----
            using var http = new HttpClient();
            var regContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = testUser,
                ["password"] = testPass,
                ["email"]    = testEmail,
            });
            var regResp = await http.PostAsync($"{ApiUrl}userapi/registration", regContent);
            regResp.EnsureSuccessStatusCode();
            var regBody = await regResp.Content.ReadAsStringAsync();
            var regJson = JsonNode.Parse(regBody);
            Assert.True((string)regJson?["error"] == null,
                $"registration failed for {testUser}: {regBody}");

            // Resolve created user_id.
            using (var conn = new MySqlConnection(DbConnTest))
            {
                await conn.OpenAsync();
                // user_id is int(10) unsigned — MySql.Data returns it as uint, not ulong.
                var rawUserId = await conn.ExecuteScalarAsync(
                    "SELECT user_id FROM fso_users WHERE username = @u LIMIT 1",
                    new { u = testUser });
                createdUserId = rawUserId is ulong ul ? (uint)ul
                              : rawUserId is uint ui  ? ui
                              : Convert.ToUInt32(rawUserId);
            }
            Assert.NotEqual(0u, createdUserId);

            // ---- Step 2: insert avatar directly into fso_avatars ----
            // head/body values are baron's known-good ea_male GUIDs (catalog-validated).
            // The server does NOT re-validate avatar visuals during purchase-lot — we only
            // need a non-anonymous session (AvatarId != 0, not Unverified). Direct insert
            // avoids Content.Init dependency (no game assets needed for this test).
            var epoch = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using (var conn = new MySqlConnection(DbConnTest))
            {
                await conn.OpenAsync();
                await conn.ExecuteAsync(
                    @"INSERT INTO fso_avatars
                        (shard_id, user_id, name, gender, date, skin_tone, head, body, description, budget)
                      VALUES
                        (@shard_id, @user_id, @name, 'male', @date, 0, @head, @body, @desc, 1000000)",
                    new
                    {
                        shard_id = 1,
                        user_id  = createdUserId,
                        name     = $"{testUser} Sim",
                        date     = epoch,
                        head     = 4081472957u,   // baron's ea_male_heads.col known-good GUID
                        body     = 4082094093u,   // baron's ea_male.col known-good GUID
                        desc     = "pl-integration-test avatar",
                    });

                var rawAvId = await conn.ExecuteScalarAsync(
                    "SELECT avatar_id FROM fso_avatars WHERE user_id = @uid LIMIT 1",
                    new { uid = createdUserId });
                createdAvatarId = rawAvId is ulong ula ? (uint)ula
                                : rawAvId is uint uia  ? uia
                                : Convert.ToUInt32(rawAvId);
            }
            Assert.NotEqual(0u, createdAvatarId);

            // ---- Step 3: auth + city connect with avatar ----
            var auth = new AuthClient(ApiUrl);
            var authResult = auth.Authenticate(new AuthRequest
            {
                Username  = testUser,
                Password  = testPass,
                ServiceID = "2",
                Version   = "Version 1.1097.1.0",
                ClientID  = "purchase-lot-integration-test",
            });
            Assert.NotNull(authResult);
            Assert.True(authResult.Valid, $"auth failed for {testUser}: {authResult.ReasonText}");

            var city = new CityClient(ApiUrl);
            var ic = city.InitialConnectServlet(new InitialConnectServletRequest
            {
                Ticket  = authResult.Ticket,
                Version = "Version 1.1097.1.0",
            });
            Assert.Equal(InitialConnectServletResultType.Authorized, ic.Status);

            var shardResp = city.ShardSelectorServlet(new ShardSelectorServletRequest
            {
                ShardName = ShardName,
                AvatarID  = createdAvatarId.ToString(),
            });
            Assert.NotNull(shardResp?.Address);

            // ---- Step 4: open Aries connection ----
            var kernel = new StandardKernel();
            kernel.Bind<IModelSerializer>().ToConstant(new ModelSerializer());
            kernel.Bind<ISerializationContext>().To<SerializationContext>().InSingletonScope();
            kernel.Bind<AriesProtocolDecoder>().ToSelf().InSingletonScope();
            kernel.Bind<AriesProtocolEncoder>().ToSelf().InSingletonScope();

            var cityAries = new AriesClient(kernel);
            var cityListener = new CityListener
            {
                ShardResp        = shardResp,
                SendClientOnline = true,
            };
            cityAries.AddSubscriber(cityListener);

            var botCmdHandler = new BotCmdHandler();
            botCmdHandler.RegisterSubscriber(cityAries);

            cityAries.Connect(shardResp.Address);

            var connected = await cityListener.WaitForClientOnlineAck(TimeSpan.FromSeconds(20));
            Assert.True(connected, "timed out waiting for city HostOnlinePDU — is grid31337-test running?");

            // Allow the server to process our ClientOnlinePDU before mutating the DB.
            await Task.Delay(500);

            // ---- Step 5: save + empty fso_neighborhoods ----
            using (var conn = new MySqlConnection(DbConnTest))
            {
                await conn.OpenAsync();
                savedNeighborhoods = (await conn.QueryAsync<DbNeighborhoodSnapshot>(
                    "SELECT neighborhood_id, name, description, shard_id, location, color, flag, guid " +
                    "FROM fso_neighborhoods")).AsList();

                await conn.ExecuteAsync("DELETE FROM fso_neighborhoods");
                neighborhoodDeleted = true;
            }

            // ---- Step 6: send purchase-lot for a purchasable tile ----
            // x=249, y=352 is a known-good purchasable tile (non-water, in-bounds, road bits
            // per existing tests: TestPurchaseLotHandlerSuccessPath uses this exact location).
            // The server passes IsPurchasable, then db.Lots.GetByLocation (lot not taken),
            // then calls db.Neighborhoods.GetByLocation which now returns null (empty table).
            // Pre-fix: NRE → untyped error string.
            // Post-fix: null guard fires → FAILED/LOT_NOT_PURCHASABLE.
            var correlationId = Guid.NewGuid().ToString();
            var cmdLine = System.Text.Json.JsonSerializer.Serialize(new
            {
                kind           = "bot-cmd",
                cmd            = "purchase-lot",
                correlation_id = correlationId,
                args           = new
                {
                    location_x  = 249,
                    location_y  = 352,
                    name        = "nre-regression-lot",
                    start_fresh = false,
                },
            });
            var node = JsonNode.Parse(cmdLine).AsObject();

            string captured = null;
            var latch = new ManualResetEventSlim();
            using var _sub = PerceptionEmitterCapture.Capture(s =>
            {
                if (s.Contains(correlationId)) { captured = s; latch.Set(); }
            });

            // Allow up to 20 seconds for the full server round-trip.
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));
            var handled = await botCmdHandler.TryHandleAsync(node, cityAries, cts.Token);
            Assert.True(handled, "TryHandleAsync must return true for purchase-lot (consumed)");

            Assert.True(latch.Wait(TimeSpan.FromSeconds(20)),
                "no bot-cmd-reply received within 20s — city server did not respond to PurchaseLotRequest");

            // ---- Step 7: assert typed response, not NRE ----
            var reply = JsonNode.Parse(captured).AsObject();
            Assert.Equal("bot-cmd-reply", (string)reply["kind"]);
            Assert.Equal(correlationId,   (string)reply["correlation_id"]);

            // C# BotCmdHandler contract: ok=true for server-side FAILED responses
            // so the Go sidecar can inspect status/reason (not just an error string).
            // See BotCmdHandlerTests.PurchaseLot_ServerLotNotPurchasable_EmitsOkTrueWithFailedStatus.
            Assert.True((bool)reply["ok"],
                $"Expected ok=true (typed server FAILED) but got ok=false. " +
                $"If error contains 'NullReferenceException' the null guard is not active. " +
                $"Full reply: {captured}");

            var data = reply["data"]?.AsObject();
            Assert.NotNull(data);

            // KEY ASSERTION: typed FAILED/LOT_NOT_PURCHASABLE, not NRE string.
            // This is the regression test for automataisland-e49:
            //   pre-fix  → status=null, reason=null, error="NullReferenceException: ..."
            //   post-fix → status="FAILED", reason="LOT_NOT_PURCHASABLE"
            Assert.Equal("FAILED", (string)data["status"]);
            Assert.Equal("LOT_NOT_PURCHASABLE", (string)data["reason"]);

            // Belt-and-suspenders: NRE string must not appear anywhere in the response.
            Assert.DoesNotContain("NullReferenceException",
                data.ToJsonString(), StringComparison.OrdinalIgnoreCase);

            // Disconnect cleanly.
            try { cityAries.Disconnect(); } catch { }
            await Task.Delay(300);
        }
        finally
        {
            // ---- Restore fso_neighborhoods ----
            if (neighborhoodDeleted && savedNeighborhoods != null && savedNeighborhoods.Count > 0)
            {
                try
                {
                    using var conn = new MySqlConnection(DbConnTest);
                    await conn.OpenAsync();
                    foreach (var nhood in savedNeighborhoods)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT IGNORE INTO fso_neighborhoods
                                (neighborhood_id, name, description, shard_id, location, color, flag, guid)
                              VALUES
                                (@neighborhood_id, @name, @description, @shard_id,
                                 @location, @color, @flag, @guid)",
                            nhood);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"[pl-inttest] WARNING: fso_neighborhoods restore failed: {ex.Message}. " +
                        "Please verify fso_neighborhoods manually.");
                }
            }

            // ---- Clean up test avatar and user ----
            await TeardownTestUser(testUser, createdAvatarId, createdUserId);
        }
    }

    // ---- Snapshot model for neighborhood save/restore ----

    private sealed class DbNeighborhoodSnapshot
    {
        public int    neighborhood_id { get; set; }
        public string name            { get; set; }
        public string description     { get; set; }
        public int    shard_id        { get; set; }
        public uint   location        { get; set; }
        public uint   color           { get; set; }
        public uint   flag            { get; set; }
        public string guid            { get; set; }
    }

    // ---- helpers ----

    private static async Task TeardownTestUser(string username, uint avatarId, uint userId)
    {
        try
        {
            using var conn = new MySqlConnection(DbConnTest);
            await conn.OpenAsync();

            if (avatarId != 0)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM fso_avatars WHERE avatar_id = @id", new { id = avatarId });
            }

            if (userId == 0 && !string.IsNullOrEmpty(username))
            {
                var rawUid = await conn.ExecuteScalarAsync(
                    "SELECT user_id FROM fso_users WHERE username = @u",
                    new { u = username });
                userId = rawUid is ulong ulv ? (uint)ulv
                       : rawUid is uint  uiv ? uiv
                       : rawUid != null ? Convert.ToUInt32(rawUid) : 0u;
            }

            if (userId != 0)
            {
                await conn.ExecuteAsync(
                    "DELETE FROM fso_users WHERE user_id = @id", new { id = userId });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[pl-inttest] teardown failed for {username}: {ex.Message}");
        }
    }

    private static string EnvOr(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { } v && v.Length > 0 ? v : fallback;
}

/// <summary>
/// Xunit collection isolates purchase-lot integration tests from other integration
/// collections (chargen-integration, search-catalog-integration) to prevent
/// parallelism issues with shared DB state (fso_neighborhoods temporarily emptied).
/// </summary>
[CollectionDefinition("purchase-lot-integration")]
public class PurchaseLotIntegrationCollection : ICollectionFixture<object> { }
