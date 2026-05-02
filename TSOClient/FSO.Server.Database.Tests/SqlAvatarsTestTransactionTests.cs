using Dapper;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.SqliteCompat;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FSO.Server.Database.Tests;

/// <summary>
/// Regression tests for SqlAvatars.TestTransaction uint.MaxValue split-query fix
/// (freesoexperiment-1a7).
///
/// Bug: When source_id == uint.MaxValue (lot fund / nobody), TestTransaction
/// issued a cross-join query with avatar_id == uint.MaxValue which never
/// matched any row, causing the result to be null and the pre-flight buy-object
/// check to report insufficient funds even when the destination had plenty.
///
/// Fix: mirrors the split-query branch from PerformTransaction — when either
/// side is uint.MaxValue, only query the real side (same pattern as the c59 H1
/// fix in PerformTransaction).
///
/// These tests run against a shared in-memory SQLite database (Mode=Memory;
/// Cache=Shared). SqliteContext takes a connection string; using Cache=Shared
/// with a named DB makes the same in-memory instance visible to all connections
/// opened with that name within the process lifetime.
/// </summary>
public class SqlAvatarsTestTransactionTests : IDisposable
{
    // Named in-memory DB — unique per test class instance so runs don't collide.
    private static int _counter;
    private readonly string _dbName;
    private readonly string _connStr;

    // Keep an anchor connection open so the named in-memory DB survives the
    // test method duration (SQLite drops the DB when the last connection closes).
    private readonly SqliteConnection _anchor;

    // Well-known IDs. Object IDs must be >= 16777216 (the srcObj threshold).
    private const uint AvatarId = 42;
    private const uint ObjectId = 16777220;

    public SqlAvatarsTestTransactionTests()
    {
        _dbName = $"SqlAvatarsTest_{System.Threading.Interlocked.Increment(ref _counter)}";
        _connStr = $"Data Source={_dbName};Mode=Memory;Cache=Shared";

        // Register Dapper type handlers the same way SqliteDAFactory does.
        // AddTypeHandler is idempotent across the process; registering twice is fine.
        SqlMapper.AddTypeHandler(new ByteHandler());
        SqlMapper.AddTypeHandler(new SbyteHandler());
        SqlMapper.AddTypeHandler(new Uint16Handler());
        SqlMapper.AddTypeHandler(new Uint32Handler());
        SqlMapper.AddTypeHandler(new Int32Handler());

        _anchor = new SqliteConnection(_connStr);
        _anchor.Open();

        // Minimal schema: only the columns TestTransaction touches.
        _anchor.Execute(@"
            CREATE TABLE IF NOT EXISTS fso_avatars (
                avatar_id INTEGER PRIMARY KEY,
                budget INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS fso_objects (
                object_id INTEGER PRIMARY KEY,
                budget INTEGER NOT NULL DEFAULT 0
            );
        ");

        _anchor.Execute(
            "INSERT INTO fso_avatars (avatar_id, budget) VALUES (@id, @budget)",
            new { id = AvatarId, budget = 500_000 });

        _anchor.Execute(
            "INSERT INTO fso_objects (object_id, budget) VALUES (@id, @budget)",
            new { id = ObjectId, budget = 250_000 });
    }

    private SqlAvatars MakeSut()
    {
        // SqliteContext is internal; InternalsVisibleTo in FSO.Server.Database.csproj
        // grants access. Each call creates a fresh context (new connection to the
        // same named DB). The context/connection is disposed when the test class
        // is torn down via the anchor connection Dispose.
        return new SqlAvatars(new SqliteContext(_connStr));
    }

    public void Dispose()
    {
        _anchor.Dispose();
    }

    // ------------------------------------------------------------------
    // Regression tests: the cross-join returned null before the fix.
    // ------------------------------------------------------------------

    /// <summary>
    /// source_id == uint.MaxValue (lot-fund purchase): the pre-flight check
    /// must return a non-null result with the destination avatar's budget.
    ///
    /// Before fix: cross-join on avatar_id == uint.MaxValue → no rows → null.
    /// After fix: query only dest side → returns dest_budget == 500_000.
    /// </summary>
    [Fact]
    public void TestTransaction_SourceIsMaxValue_ReturnsNonNullWithDestBudget()
    {
        var sut = MakeSut();
        var result = sut.TestTransaction(uint.MaxValue, AvatarId, 100, 0);

        Assert.NotNull(result);
        Assert.Equal(500_000, result.dest_budget);
        Assert.Equal(0, result.source_budget); // lot-fund side returns synthetic 0
        Assert.True(result.success);           // dest has 500k >= 100
    }

    /// <summary>
    /// dest_id == uint.MaxValue (lot-fund destination): mirrors the source case.
    /// Before fix: same null result. After fix: returns source_budget == 500_000.
    /// </summary>
    [Fact]
    public void TestTransaction_DestIsMaxValue_ReturnsNonNullWithSourceBudget()
    {
        var sut = MakeSut();
        var result = sut.TestTransaction(AvatarId, uint.MaxValue, 100, 0);

        Assert.NotNull(result);
        Assert.Equal(500_000, result.source_budget);
        Assert.Equal(0, result.dest_budget);
        Assert.True(result.success);
    }

    /// <summary>
    /// Both sides uint.MaxValue: result must be non-null (trivially succeeds —
    /// no real rows to check).
    /// </summary>
    [Fact]
    public void TestTransaction_BothMaxValue_ReturnsNonNull()
    {
        var sut = MakeSut();
        var result = sut.TestTransaction(uint.MaxValue, uint.MaxValue, 0, 0);
        Assert.NotNull(result);
    }

    // ------------------------------------------------------------------
    // Correctness: real IDs still work after the refactor.
    // ------------------------------------------------------------------

    [Fact]
    public void TestTransaction_BothRealIds_ReturnsBothBudgets()
    {
        var sut = MakeSut();
        var result = sut.TestTransaction(AvatarId, ObjectId, 1000, 0);

        Assert.NotNull(result);
        Assert.Equal(500_000, result.source_budget);
        Assert.Equal(250_000, result.dest_budget);
        Assert.True(result.success);
    }

    [Fact]
    public void TestTransaction_InsufficientSourceFunds_SuccessIsFalse()
    {
        var sut = MakeSut();
        // Transfer more than the source has → success should be false.
        var result = sut.TestTransaction(AvatarId, ObjectId, 1_000_000, 0);

        Assert.NotNull(result);
        Assert.False(result.success);
    }
}
