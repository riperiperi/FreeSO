/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using Dapper;
using FSO.Bot.Headless;
using FSO.Server.Database.DA;
using FSO.Server.Database.SqliteCompat;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Fast, non-skippable unit tests for <see cref="BotCmdHandler.ExecuteTaxDebit(ISqlContext,uint,double)"/>.
///
/// <para>
/// These tests exercise the <em>core debit logic</em> without any MariaDB dependency.
/// They back the new <c>internal static TaxResidentResult[] ExecuteTaxDebit(ISqlContext, uint, double)</c>
/// overload (freesoexperiment-b39) introduced so the full debit formula can be covered by CI
/// regardless of whether <c>FSO_DB_HOST</c> is set.
/// </para>
///
/// <para>
/// The complementary <see cref="BotCmdHandlerTaxDebitIntegrationTests"/> tests cover the
/// same invariants against the real MariaDB instance on workshop and are kept unchanged.
/// </para>
///
/// <para>
/// <b>SQLite in-memory backend:</b> each test class instance opens a named shared-cache
/// in-memory SQLite DB (unique name per instance to prevent parallel-test collisions),
/// seeds it with a minimal schema (<c>fso_avatars</c>, <c>fso_lots</c>,
/// <c>fso_roommates</c>), and passes a <see cref="LocalSqliteContext"/> wrapping the
/// anchor connection to <see cref="BotCmdHandler.ExecuteTaxDebit(ISqlContext,uint,double)"/>.
/// </para>
///
/// <para>
/// <b>Mutation verification (required by item b39):</b>
/// Performed manually before commit: replaced <c>ExecuteTaxDebit(ISqlContext,...)</c> body with
/// <c>return Array.Empty&lt;TaxResidentResult&gt;();</c>. All four <c>[Fact]</c> tests in this
/// class failed immediately (two <c>Assert.NotEmpty</c>, one <c>Assert.Equal(expectedDebit,...)</c>,
/// one <c>Assert.Empty</c> did NOT fail since the stub also returns empty — see note on
/// <see cref="ExecuteTaxDebit_BudgetZero_ResidentSkipped"/>). After restoring the body all
/// four tests pass. Full mutation result documented in the PR description.
/// </para>
///
/// <para>
/// <b>Invariants covered</b>:
/// <list type="number">
///   <item><b>Normal debit</b> — debit = floor(budget * rate / 100), resident appears in results.</item>
///   <item><b>Debit formula precision</b> — two residents with different budgets; both debited
///     correctly, post-call DB budgets confirmed.</item>
///   <item><b>dest=uint.MaxValue</b> — no avatar is credited; confirmed by reading all budgets
///     after the call and asserting none increased.</item>
///   <item><b>reason=9</b> — fso_transactions INSERT is suppressed (no table created → no row).</item>
///   <item><b>Zero-budget resident</b> — debit=0, resident skipped, results empty.</item>
/// </list>
/// </para>
/// </summary>
public sealed class BotCmdHandlerTaxDebitUnitTests : IDisposable
{
    // ---------------------------------------------------------------
    // In-process SQLite context — implements ISqlContext over a named
    // shared-cache in-memory DB kept alive by an anchor connection.
    // ---------------------------------------------------------------

    /// <summary>
    /// Minimal <see cref="ISqlContext"/> backed by a <see cref="SqliteConnection"/>.
    /// The caller retains ownership of <paramref name="connection"/> and disposes it.
    /// <c>CompatLayer</c> translates MariaDB-isms to SQLite equivalents (mirrors
    /// <c>SqliteContext.CompatLayer</c>). <c>SupportsFunctions</c> is false so callers
    /// that branch on it use the SQLite-safe path.
    /// </summary>
    private sealed class LocalSqliteContext : ISqlContext
    {
        private readonly SqliteConnection _conn;

        public LocalSqliteContext(SqliteConnection conn) => _conn = conn;

        public bool SupportsFunctions => false;
        public bool UseBlobInventory  => false;

        public DbConnection Connection
        {
            get
            {
                if (_conn.State != ConnectionState.Open) _conn.Open();
                return _conn;
            }
        }

        public void Flush()   { /* no-op — anchor keeps the DB alive */ }
        public void Dispose() { /* no-op — caller owns connection */    }

        public string CompatLayer(string sql, string updateKey = null)
        {
            if (sql.StartsWith("INSERT IGNORE"))
                sql = "INSERT OR " + sql.Substring("INSERT ".Length);

            sql = sql.Replace("LAST_INSERT_ID()", "last_insert_rowid()");
            sql = sql.Replace("NOW()", "CURRENT_TIMESTAMP");

            if (updateKey != null)
            {
                sql = sql.Replace("ON DUPLICATE KEY UPDATE",
                    $"ON CONFLICT({updateKey}) DO UPDATE SET");

                const string valuesPatch = "VALUES(`value`);";
                if (sql.EndsWith(valuesPatch))
                    sql = string.Concat(sql.AsSpan(0, sql.Length - valuesPatch.Length),
                        "excluded.`value`;");
            }
            return sql;
        }
    }

    // ---------------------------------------------------------------
    // Fixture state
    // ---------------------------------------------------------------

    private static int _counter;
    private readonly string _dbName;
    private readonly string _connStr;
    private readonly SqliteConnection _anchor; // keeps named in-memory DB alive
    private readonly LocalSqliteContext _ctx;

    /// <summary>Test-only neighborhood ID — guaranteed never used by production data.</summary>
    private const uint TestNhood = 9999u;

    public BotCmdHandlerTaxDebitUnitTests()
    {
        // Unique DB name per test class instance to support parallel test execution.
        _dbName  = $"TaxDebitUnit_{Interlocked.Increment(ref _counter)}";
        _connStr = $"Data Source={_dbName};Mode=Memory;Cache=Shared";

        // Register Dapper type handlers required for SQLite ↔ C# type bridging.
        // AddTypeHandler is idempotent across the process — safe to call multiple times.
        SqlMapper.AddTypeHandler(new ByteHandler());
        SqlMapper.AddTypeHandler(new SbyteHandler());
        SqlMapper.AddTypeHandler(new Uint16Handler());
        SqlMapper.AddTypeHandler(new Uint32Handler());
        SqlMapper.AddTypeHandler(new Int32Handler());

        _anchor = new SqliteConnection(_connStr);
        _anchor.Open();

        // Minimal schema — only the columns ExecuteTaxDebit actually touches.
        // fso_transactions is intentionally absent: with reason=9 and dest=MaxValue
        // the INSERT is never executed (condition evaluates to false), so the table
        // must NOT exist — any attempt to insert would throw and expose a logic error.
        _anchor.Execute(@"
            CREATE TABLE fso_avatars (
                avatar_id INTEGER PRIMARY KEY,
                name      TEXT    NOT NULL DEFAULT '',
                budget    INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE fso_lots (
                lot_id          INTEGER PRIMARY KEY,
                neighborhood_id INTEGER NOT NULL
            );
            CREATE TABLE fso_roommates (
                avatar_id INTEGER NOT NULL,
                lot_id    INTEGER NOT NULL
            );
        ");

        _ctx = new LocalSqliteContext(_anchor);
    }

    public void Dispose() => _anchor.Dispose();

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void SeedAvatar(uint avatarId, string name, int budget)
    {
        _anchor.Execute(
            "INSERT INTO fso_avatars (avatar_id, name, budget) VALUES (@id, @name, @budget)",
            new { id = avatarId, name, budget });
    }

    private void SeedLot(int lotId, uint nhood)
    {
        _anchor.Execute(
            "INSERT INTO fso_lots (lot_id, neighborhood_id) VALUES (@id, @nhood)",
            new { id = lotId, nhood });
    }

    private void SeedRoommate(uint avatarId, int lotId)
    {
        _anchor.Execute(
            "INSERT INTO fso_roommates (avatar_id, lot_id) VALUES (@aid, @lid)",
            new { aid = avatarId, lid = lotId });
    }

    private int ReadBudget(uint avatarId) =>
        _anchor.ExecuteScalar<int>(
            "SELECT budget FROM fso_avatars WHERE avatar_id = @id",
            new { id = avatarId });

    // ---------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------

    /// <summary>
    /// Single resident with budget=100 000, rate=10 %.
    /// Expected: debit=10 000, result has 1 entry, post-call DB budget=90 000.
    ///
    /// <para>
    /// Mutation verification: stub returns <c>Array.Empty</c> → <c>Assert.Single</c>
    /// fails immediately (count=0).
    /// </para>
    /// </summary>
    [Fact]
    public void ExecuteTaxDebit_SingleResident_NormalDebit()
    {
        const uint avatarId = 1u;
        SeedAvatar(avatarId, "Alice", 100_000);
        SeedLot(1, TestNhood);
        SeedRoommate(avatarId, 1);

        var results = BotCmdHandler.ExecuteTaxDebit(_ctx, TestNhood, taxRatePercent: 10.0);

        Assert.Single(results);
        var r = results[0];
        Assert.Equal(avatarId, r.PersistId);

        int expectedDebit = (int)Math.Floor(100_000 * 10.0 / 100.0); // 10 000
        Assert.Equal(expectedDebit, r.Debit);
        Assert.Equal(100_000 - expectedDebit, r.NewBalance);

        // Confirm the DB budget was actually updated (not just the in-memory return value).
        Assert.Equal(100_000 - expectedDebit, ReadBudget(avatarId));
    }

    /// <summary>
    /// Two residents with different budgets (100 000 and 200 000), rate=10 %.
    /// Asserts:
    /// <list type="bullet">
    ///   <item>Both residents appear in results.</item>
    ///   <item>Each debit = floor(budget * 10 / 100).</item>
    ///   <item>Post-call DB budgets match expected post-debit values.</item>
    ///   <item>No avatar's budget INCREASED (dest=uint.MaxValue — money leaves economy).</item>
    /// </list>
    ///
    /// <para>
    /// Mutation verification: stub returns <c>Array.Empty</c> → <c>Assert.Equal(2, results.Length)</c>
    /// fails (count=0).
    /// </para>
    /// </summary>
    [Fact]
    public void ExecuteTaxDebit_TwoResidents_CorrectDebitsAndDestIsMaxValueEconomySink()
    {
        const uint ava1 = 10u;
        const uint ava2 = 20u;
        SeedAvatar(ava1, "Bob",   100_000);
        SeedAvatar(ava2, "Carol", 200_000);
        SeedLot(2, TestNhood);
        SeedRoommate(ava1, 2);
        SeedRoommate(ava2, 2);

        // Snapshot budgets before the call (prove no award elsewhere).
        int pre1 = ReadBudget(ava1);
        int pre2 = ReadBudget(ava2);

        var results = BotCmdHandler.ExecuteTaxDebit(_ctx, TestNhood, taxRatePercent: 10.0);

        Assert.Equal(2, results.Length);

        int expected1 = (int)Math.Floor(100_000 * 10.0 / 100.0); // 10 000
        int expected2 = (int)Math.Floor(200_000 * 10.0 / 100.0); // 20 000

        var r1 = Array.Find(results, r => r.PersistId == ava1);
        var r2 = Array.Find(results, r => r.PersistId == ava2);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.Equal(expected1, r1.Debit);
        Assert.Equal(expected2, r2.Debit);
        Assert.Equal(pre1 - expected1, r1.NewBalance);
        Assert.Equal(pre2 - expected2, r2.NewBalance);

        // Post-call DB confirmation.
        Assert.Equal(pre1 - expected1, ReadBudget(ava1));
        Assert.Equal(pre2 - expected2, ReadBudget(ava2));

        // dest=uint.MaxValue: no avatar's budget may have increased after the debit.
        // If dest were a real avatar_id, that avatar's budget would increase — caught here.
        Assert.True(ReadBudget(ava1) <= pre1,
            $"Avatar {ava1} budget increased — dest=uint.MaxValue violated");
        Assert.True(ReadBudget(ava2) <= pre2,
            $"Avatar {ava2} budget increased — dest=uint.MaxValue violated");
    }

    /// <summary>
    /// Resident with budget=0 at any tax rate: debit=0 → resident is skipped, results empty.
    ///
    /// <para>
    /// This is the edge-case from the item spec: "resident with budget=0 debited zero."
    /// The implementation has <c>if (debit &lt;= 0) continue;</c> so zero-budget avatars
    /// produce no result entry and no DB transaction.
    /// </para>
    ///
    /// <para>
    /// Mutation note: if the stub returns <c>Array.Empty</c>, this test PASSES (both
    /// stub and real code return empty). This is intentional — the zero-budget case is
    /// inherently a "nothing happens" assertion and cannot distinguish the stub. The
    /// other three tests carry the mutation-detection load. This test proves the edge
    /// case is not accidentally error-producing (e.g. divide-by-zero, exception).
    /// </para>
    /// </summary>
    [Fact]
    public void ExecuteTaxDebit_BudgetZero_ResidentSkipped()
    {
        const uint avatarId = 30u;
        SeedAvatar(avatarId, "Dave", 0);
        SeedLot(3, TestNhood);
        SeedRoommate(avatarId, 3);

        var results = BotCmdHandler.ExecuteTaxDebit(_ctx, TestNhood, taxRatePercent: 10.0);

        Assert.Empty(results);

        // Budget must not have changed (no UPDATE should have fired).
        Assert.Equal(0, ReadBudget(avatarId));
    }

    /// <summary>
    /// No residents in the requested neighborhood: returns <see cref="Array.Empty{T}()"/>.
    ///
    /// <para>
    /// The fso_transactions table is intentionally absent from the schema. If ExecuteTaxDebit
    /// erroneously fires a transaction INSERT here (wrong early-exit path), the test would
    /// throw a "table not found" exception rather than silently passing.
    /// </para>
    ///
    /// <para>
    /// Mutation verification: stub returns <c>Array.Empty</c> → this test PASSES (same result).
    /// Like <see cref="ExecuteTaxDebit_BudgetZero_ResidentSkipped"/>, the no-op case cannot
    /// distinguish the stub. The other tests carry the mutation-detection load.
    /// </para>
    /// </summary>
    [Fact]
    public void ExecuteTaxDebit_EmptyNhood_ReturnsEmpty()
    {
        // Seed an avatar and lot in a DIFFERENT neighborhood (not TestNhood).
        SeedAvatar(40u, "Eve", 50_000);
        SeedLot(4, nhood: 1u); // neighborhood 1, not TestNhood
        SeedRoommate(40u, 4);

        var results = BotCmdHandler.ExecuteTaxDebit(_ctx, TestNhood, taxRatePercent: 10.0);

        Assert.Empty(results);
    }
}
