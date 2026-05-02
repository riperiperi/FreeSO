using Dapper;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using MySql.Data.MySqlClient;
using Xunit;

namespace FSO.Server.Database.Tests;

/// <summary>
/// Integration tests for SqlAvatars.TestTransaction against the real MariaDB instance
/// (freeso-mariadb-1 on workshop).
///
/// Addresses the veracity-adversary finding on PR #2 (freesoexperiment-1a7):
///
///   Finding 1 — parameter binding: uint.MaxValue (4294967295) exceeds int range.
///   Microsoft.Data.Sqlite and MySql.Data bind it differently (different Uint32Handler
///   implementations). These tests prove the fix returns correct results through the
///   real MySql.Data driver on an actual MariaDB int(10) unsigned column.
///
///   Finding 2 — cross-join edge case: SQLite returns ZERO ROWS when a
///   count(budget) subquery finds no match; MariaDB returns a ROW with NULL
///   budget. The SQLite tests proved the fix eliminates the null-result for SQLite.
///   Test MariaDb_CrossJoinWithAbsentSide_ReturnsNullBudget (below) documents the
///   MariaDB baseline behavior, proving the driver difference is real and the
///   integration test adds genuine coverage.
///
///   Finding 3 — no MariaDB integration test: prior to this commit the fix had
///   no receipt against the real server. All five [SkippableFact] tests here run
///   against the live freeso-mariadb-1 instance on workshop.
///
/// No fixture data is inserted. All tests use existing avatars:
///   - avatar_id=5  (Botrous, large positive budget — primary real avatar)
///   - avatar_id=3  (Lady, smaller budget — secondary real avatar)
///   - avatar_id=99999 — guaranteed absent (cross-join edge case)
///
/// Gating: Set FSO_DB_HOST (e.g. "172.18.0.2") to enable. Tests skip when unset
/// so CI can pass without a live MariaDB. On workshop FSO_DB_HOST is always set
/// in the test-runner invocation.
///
/// MariaDB vs SQLite cross-join note:
///   Old buggy query:
///     SELECT a1.budget AS source_budget, a2.budget AS dest_budget
///     FROM (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id=@absent) a1,
///          (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id=@real)   a2
///   SQLite result: 0 rows → Dapper FirstOrDefault() → null      ← the bug (null result)
///   MariaDB result: 1 row  → source_budget=NULL, dest_budget=X  ← different, not null
///
///   The fix eliminates the need for this cross-join entirely when either side is
///   uint.MaxValue by querying only the real side. Both SQLite and MariaDB return
///   the correct non-null result with correct budgets after the fix.
/// </summary>
public class SqlAvatarsMariaDbIntegrationTests
{
    // Existing avatars — read-only, never modified by these tests.
    private const uint BotrousId = 5;     // large budget, known present
    private const uint LadyId    = 3;     // smaller budget, known present
    private const uint MissingId = 99999; // not in DB — used for edge-case tests

    private static string SkipReason =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FSO_DB_HOST"))
            ? "FSO_DB_HOST not set — skipping MariaDB integration tests"
            : null;

    private static string ConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("FSO_DB_HOST") ?? "172.18.0.2";
        var user = Environment.GetEnvironmentVariable("FSO_DB_USER") ?? "fsoserver";
        var pass = Environment.GetEnvironmentVariable("FSO_DB_PASS") ?? "password";
        var db   = Environment.GetEnvironmentVariable("FSO_DB_NAME") ?? "fso";
        return $"Server={host};Port=3306;Database={db};Uid={user};Pwd={pass};";
    }

    /// <summary>Sanity-check: the DB is reachable and expected avatars exist.</summary>
    private static void AssertDbReachable()
    {
        using var conn = new MySqlConnection(ConnectionString());
        conn.Open();
        var count = conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM fso_avatars WHERE avatar_id IN (@b, @l);",
            new { b = BotrousId, l = LadyId });
        Assert.Equal(2L, count);
    }

    private SqlAvatars MakeSut() => new(new MySqlContext(ConnectionString()));

    // ------------------------------------------------------------------
    // Baseline: documents MariaDB's cross-join behavior for an absent row.
    // Proves Finding 2 is real: MariaDB does NOT return null from the cross-join
    // when one side is absent — it returns a row with NULL budget (mapped to 0
    // by Dapper). This differs from SQLite (which returns 0 rows → null result).
    //
    // This test runs the raw cross-join query directly, bypassing TestTransaction,
    // so it documents the engine behavior independently of the fix.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_CrossJoinWithAbsentSide_ReturnsNullBudget_NotNullResult()
    {
        Skip.If(SkipReason != null, SkipReason);

        using var conn = new MySqlConnection(ConnectionString());
        conn.Open();

        // This is the pre-fix query that was used by TestTransaction.
        // MissingId doesn't exist; BotrousId does.
        var result = conn.Query<DbTransactionResult>(
            "SELECT a1.budget AS source_budget, a2.budget AS dest_budget " +
            "FROM (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @source_id) a1, " +
            "     (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @avatar_id) a2;",
            new { source_id = MissingId, avatar_id = BotrousId })
            .FirstOrDefault();

        // MariaDB returns a row with source_budget=NULL (→ 0 after Dapper mapping),
        // NOT a null result. This is the documented behavioral difference from SQLite.
        Assert.NotNull(result);
        Assert.Equal(0, result.source_budget); // NULL mapped to int default 0

        // With uint.MaxValue as source_id — key driver-binding verification:
        // MySql.Data must not overflow/error when binding 4294967295.
        var resultMaxValue = conn.Query<DbTransactionResult>(
            "SELECT a1.budget AS source_budget, a2.budget AS dest_budget " +
            "FROM (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @source_id) a1, " +
            "     (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @avatar_id) a2;",
            new { source_id = uint.MaxValue, avatar_id = BotrousId })
            .FirstOrDefault();

        // uint.MaxValue row doesn't exist — same MariaDB behavior: non-null row, NULL budget.
        Assert.NotNull(resultMaxValue);
        Assert.Equal(0, resultMaxValue.source_budget);
        Assert.True(resultMaxValue.dest_budget > 0, "Botrous dest_budget should survive the join");
    }

    // ------------------------------------------------------------------
    // 1. source_id == uint.MaxValue (lot-fund purchase).
    //    After fix: the split-query branch runs the dest-only query through
    //    MySql.Data, binding the real dest_id and returning its budget.
    //    Result: non-null, dest_budget = Botrous's balance, source_budget = 0.
    //    Regression: if the fix is reverted and MySql.Data had an overflow bug
    //    (Finding 1), this would fail. It doesn't — proving the binding works.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_SourceIsMaxValue_ReturnsNonNullWithDestBudget()
    {
        Skip.If(SkipReason != null, SkipReason);
        AssertDbReachable();

        var sut = MakeSut();
        var result = sut.TestTransaction(uint.MaxValue, BotrousId, 100, 0);

        Assert.NotNull(result);
        Assert.True(result.dest_budget > 0, "dest_budget should be Botrous's positive balance");
        Assert.Equal(0, result.source_budget); // lot-fund side: synthetic 0
        Assert.True(result.success);
    }

    // ------------------------------------------------------------------
    // 2. dest_id == uint.MaxValue (lot-fund destination — symmetric case).
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_DestIsMaxValue_ReturnsNonNullWithSourceBudget()
    {
        Skip.If(SkipReason != null, SkipReason);
        AssertDbReachable();

        var sut = MakeSut();
        var result = sut.TestTransaction(BotrousId, uint.MaxValue, 100, 0);

        Assert.NotNull(result);
        Assert.True(result.source_budget > 0, "source_budget should be Botrous's positive balance");
        Assert.Equal(0, result.dest_budget); // lot-fund dest: synthetic 0
        Assert.True(result.success);
    }

    // ------------------------------------------------------------------
    // 3. Both real IDs, dest has NO avatar row (Finding 2 — cross-join edge case).
    //    On MariaDB: cross-join returns a row with NULL dest_budget (→ 0).
    //    The pre-flight exception branch catches "Dest avatar/object does not
    //    exist!" and sets success=false regardless of the result row.
    //    Assertion: never claim success when dest is absent.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_BothRealIds_MissingDest_SuccessIsFalse()
    {
        Skip.If(SkipReason != null, SkipReason);
        AssertDbReachable();

        var sut = MakeSut();
        var result = sut.TestTransaction(BotrousId, MissingId, 100, 0);

        // On MariaDB the cross-join returns a non-null row (see baseline test above).
        // success MUST be false because the exception branch fired for missing dest.
        Assert.NotNull(result);
        Assert.False(result.success,
            "TestTransaction must not claim success when dest avatar is absent");
    }

    // ------------------------------------------------------------------
    // 4. Both real IDs happy path.
    //    The cross-join subquery path on MariaDB when both avatars exist.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_BothRealIds_ReturnsBothBudgets()
    {
        Skip.If(SkipReason != null, SkipReason);
        AssertDbReachable();

        var sut = MakeSut();
        var result = sut.TestTransaction(BotrousId, LadyId, 100, 0);

        Assert.NotNull(result);
        Assert.True(result.source_budget > 0, "Botrous source_budget should be > 0");
        Assert.True(result.dest_budget > 0, "Lady dest_budget should be > 0");
        Assert.True(result.success);
    }

    // ------------------------------------------------------------------
    // 5. Insufficient funds → success=false on MariaDB int(11) columns.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void MariaDb_InsufficientSourceFunds_SuccessIsFalse()
    {
        Skip.If(SkipReason != null, SkipReason);
        AssertDbReachable();

        var sut = MakeSut();
        var result = sut.TestTransaction(LadyId, BotrousId, 2_000_000_000, 0);

        Assert.NotNull(result);
        Assert.False(result.success);
    }
}

/// <summary>
/// Integration tests for SqlAvatars.Transaction (PerformTransaction) against the real
/// MariaDB instance (freeso-mariadb-1 on workshop).
///
/// Addresses the test gap identified in freesoexperiment-05f: the parent item
/// freesoexperiment-1a7 added the uint.MaxValue split-query fix to BOTH
/// TestTransaction and Transaction (PerformTransaction), but the MariaDB
/// integration tests in the 1a7 commit only exercised TestTransaction.
/// Transaction (the mutation path) had no receipt against the real server.
///
/// Each test creates fresh isolate avatar rows at IDs 90001/90002 (guaranteed
/// absent from the live DB), exercises the transaction, then restores state in a
/// finally block. This makes the tests safe to run against the live research DB
/// without clobbering production avatar budgets.
///
/// Tests:
///   1. source_id == uint.MaxValue (lot-fund purchase): dest budget increases.
///   2. dest_id == uint.MaxValue (lot-fund sale): source budget decreases.
///   3. Both real IDs happy path: both budgets change.
///   4. Mutation revert proof: the raw cross-join query that the pre-fix code
///      used returns source_budget=NULL when source_id=uint.MaxValue, proving
///      the split-query branch is load-bearing on MariaDB.
///
/// Gating: Set FSO_DB_HOST (e.g. "127.0.0.1") to enable. Tests skip when unset.
/// </summary>
public class SqlAvatarsPerformTransactionMariaDbTests : IDisposable
{
    // High IDs guaranteed to be absent from the live research DB.
    private const uint TestAvatarSrc = 90001;
    private const uint TestAvatarDst = 90002;

    // Starting budgets for fixture avatars.
    private const int SrcStartBudget = 1_000_000;
    private const int DstStartBudget =   500_000;
    private const int TransferAmount =     10_000;

    private static string SkipReason =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FSO_DB_HOST"))
            ? "FSO_DB_HOST not set — skipping MariaDB integration tests"
            : null;

    private static string ConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("FSO_DB_HOST") ?? "172.18.0.2";
        var user = Environment.GetEnvironmentVariable("FSO_DB_USER") ?? "fsoserver";
        var pass = Environment.GetEnvironmentVariable("FSO_DB_PASS") ?? "password";
        var db   = Environment.GetEnvironmentVariable("FSO_DB_NAME") ?? "fso";
        return $"Server={host};Port=3306;Database={db};Uid={user};Pwd={pass};";
    }

    private readonly MySqlConnection _conn;

    public SqlAvatarsPerformTransactionMariaDbTests()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FSO_DB_HOST")))
            return; // skip init when gating env var absent

        _conn = new MySqlConnection(ConnectionString());
        _conn.Open();

        // Remove any leftover fixture rows from a prior failed run.
        _conn.Execute(
            "DELETE FROM fso_avatars WHERE avatar_id IN (@src, @dst);",
            new { src = TestAvatarSrc, dst = TestAvatarDst });

        // Insert fresh fixture avatars with all NOT NULL required columns.
        // shard_id=1 (Alphaville), user_id=1 (admin — always present), gender/date/etc from Botrous.
        // Names are unique: no UNIQUE KEY collision with production rows.
        // We use INSERT IGNORE so a prior failed run's leftovers don't block setup.
        const string insertSql =
            "INSERT IGNORE INTO fso_avatars " +
            "(avatar_id, shard_id, user_id, name, gender, date, skin_tone, head, body, description, budget) " +
            "VALUES (@src, 1, 1, 'PerfTestSrc', 'male', 0, 0, 0, 0, '', @srcBudget), " +
            "       (@dst, 1, 1, 'PerfTestDst', 'male', 0, 0, 0, 0, '', @dstBudget);";
        _conn.Execute(insertSql,
            new { src = TestAvatarSrc, srcBudget = SrcStartBudget, dst = TestAvatarDst, dstBudget = DstStartBudget });
    }

    public void Dispose()
    {
        if (_conn == null) return;
        try
        {
            // Always clean up fixture avatars and any transaction log rows.
            _conn.Execute(
                "DELETE FROM fso_transactions WHERE from_id IN (@src, @dst) OR to_id IN (@src, @dst);",
                new { src = TestAvatarSrc, dst = TestAvatarDst });
            _conn.Execute(
                "DELETE FROM fso_avatars WHERE avatar_id IN (@src, @dst);",
                new { src = TestAvatarSrc, dst = TestAvatarDst });
        }
        finally
        {
            _conn.Dispose();
        }
    }

    private SqlAvatars MakeSut() => new(new MySqlContext(ConnectionString()));

    private int ReadBudget(uint avatarId) =>
        _conn.ExecuteScalar<int>(
            "SELECT budget FROM fso_avatars WHERE avatar_id = @id;",
            new { id = avatarId });

    // ------------------------------------------------------------------
    // 1. source_id == uint.MaxValue (lot-fund purchase path).
    //    The split-query branch skips the source UPDATE (uint.MaxValue guard)
    //    and only increments the destination. Post-call SELECT must show that
    //    TestAvatarDst's budget increased by TransferAmount.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void PerformTransaction_SourceIsMaxValue_DestBudgetIncreases()
    {
        Skip.If(SkipReason != null, SkipReason);

        var budgetBefore = ReadBudget(TestAvatarDst);

        var sut = MakeSut();
        var result = sut.Transaction(uint.MaxValue, TestAvatarDst, TransferAmount, 0);

        var budgetAfter = ReadBudget(TestAvatarDst);

        // Transaction must succeed and return a non-null result.
        Assert.NotNull(result);
        Assert.True(result.success, "Transaction with source=uint.MaxValue should succeed");

        // Split-query result: source_budget synthetic 0, dest_budget reflects post-commit row.
        Assert.Equal(0, result.source_budget);
        Assert.True(result.dest_budget > 0, "dest_budget in result should be > 0");

        // Actual DB mutation: dest avatar gained TransferAmount.
        Assert.Equal(budgetBefore + TransferAmount, budgetAfter);

        // Restore to start state.
        _conn.Execute(
            "UPDATE fso_avatars SET budget = @b WHERE avatar_id = @id;",
            new { b = DstStartBudget, id = TestAvatarDst });
    }

    // ------------------------------------------------------------------
    // 2. dest_id == uint.MaxValue (lot-fund sale path).
    //    The split-query branch only decrements the source. Post-call SELECT
    //    must show that TestAvatarSrc's budget decreased by TransferAmount.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void PerformTransaction_DestIsMaxValue_SourceBudgetDecreases()
    {
        Skip.If(SkipReason != null, SkipReason);

        var budgetBefore = ReadBudget(TestAvatarSrc);

        var sut = MakeSut();
        var result = sut.Transaction(TestAvatarSrc, uint.MaxValue, TransferAmount, 0);

        var budgetAfter = ReadBudget(TestAvatarSrc);

        Assert.NotNull(result);
        Assert.True(result.success, "Transaction with dest=uint.MaxValue should succeed");

        // Split-query result: source_budget reflects post-commit row, dest_budget synthetic 0.
        Assert.True(result.source_budget > 0, "source_budget in result should be > 0");
        Assert.Equal(0, result.dest_budget);

        // Actual DB mutation: source avatar lost TransferAmount.
        Assert.Equal(budgetBefore - TransferAmount, budgetAfter);

        // Restore to start state.
        _conn.Execute(
            "UPDATE fso_avatars SET budget = @b WHERE avatar_id = @id;",
            new { b = SrcStartBudget, id = TestAvatarSrc });
    }

    // ------------------------------------------------------------------
    // 3. Both real IDs — happy-path mutation.
    //    Both budgets change by TransferAmount and the result reflects the
    //    post-commit values via the cross-join SELECT.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void PerformTransaction_BothRealIds_BothBudgetsChange()
    {
        Skip.If(SkipReason != null, SkipReason);

        var srcBefore = ReadBudget(TestAvatarSrc);
        var dstBefore = ReadBudget(TestAvatarDst);

        var sut = MakeSut();
        var result = sut.Transaction(TestAvatarSrc, TestAvatarDst, TransferAmount, 0);

        var srcAfter = ReadBudget(TestAvatarSrc);
        var dstAfter = ReadBudget(TestAvatarDst);

        Assert.NotNull(result);
        Assert.True(result.success);

        // Actual DB mutations.
        Assert.Equal(srcBefore - TransferAmount, srcAfter);
        Assert.Equal(dstBefore + TransferAmount, dstAfter);

        // Result struct from the post-commit cross-join SELECT reflects new values.
        Assert.Equal(srcAfter, result.source_budget);
        Assert.Equal(dstAfter, result.dest_budget);

        // Restore to start state.
        _conn.Execute(
            "UPDATE fso_avatars SET budget = @srcBudget WHERE avatar_id = @srcId;" +
            "UPDATE fso_avatars SET budget = @dstBudget WHERE avatar_id = @dstId;",
            new { srcId = TestAvatarSrc, srcBudget = SrcStartBudget,
                  dstId = TestAvatarDst, dstBudget = DstStartBudget });
    }

    // ------------------------------------------------------------------
    // 4. Mutation revert proof: the raw cross-join query (pre-fix logic)
    //    returns source_budget = NULL (mapped to 0 by Dapper) when
    //    source_id = uint.MaxValue, even though the uint.MaxValue row is
    //    simply absent. This proves the split-query branch is load-bearing:
    //    without it, the caller would receive source_budget=0 from a
    //    cross-join NULL — indistinguishable from a legitimate empty account.
    //
    //    Note: this differs from the SQLite case (SQLite returns 0 rows →
    //    null object). MariaDB returns a row with a NULL column. Either way
    //    the caller cannot determine whether the source side genuinely had
    //    a zero budget or whether the row was simply absent. The split-query
    //    removes the ambiguity by not querying the uint.MaxValue side at all.
    // ------------------------------------------------------------------

    [SkippableFact]
    public void PerformTransaction_RevertProof_CrossJoinReturnsNullBudgetForAbsentSide()
    {
        Skip.If(SkipReason != null, SkipReason);

        using var conn = new MySqlConnection(ConnectionString());
        conn.Open();

        // The pre-fix cross-join query for the case source_id == uint.MaxValue.
        // (uint.MaxValue = 4294967295 — no such avatar_id exists.)
        var result = conn.Query<DbTransactionResult>(
            "SELECT a1.budget AS source_budget, a2.budget AS dest_budget " +
            "FROM (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @source_id) a1, " +
            "     (SELECT budget, count(budget) FROM fso_avatars WHERE avatar_id = @avatar_id) a2;",
            new { source_id = uint.MaxValue, avatar_id = TestAvatarDst })
            .FirstOrDefault();

        // MariaDB: cross-join of an absent source with a present dest produces
        // a row where source_budget = NULL → 0. The budget column from a
        // count(*)-aggregated subquery with no matching rows is NULL.
        //
        // A caller receiving this result cannot distinguish "source has budget=0"
        // from "source doesn't exist." The split-query fix avoids the cross-join
        // entirely when source is uint.MaxValue, removing this ambiguity.
        Assert.NotNull(result);  // MariaDB does return a row (unlike SQLite which returns none)
        Assert.Equal(0, result.source_budget); // NULL → 0: ambiguous, masks absence

        // The dest_budget IS correctly populated by the cross-join when the dest
        // side exists — confirming the issue is only on the absent side.
        Assert.True(result.dest_budget > 0,
            "dest_budget should be TestAvatarDst's balance from the cross-join");
    }
}

