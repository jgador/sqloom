using System.Collections.Generic;
using Sqloom.TestApp.Harness;
using Xunit;

namespace Sqloom.UnitTests.TestApp;

/// <summary>
/// Exercises SQL seed script execution helpers used by the sample replay harness.
/// </summary>
public sealed class TestAppReplaySqlServerSeedScriptExecutorTests
{
    [Fact]
    public void SplitBatches_SplitsOnStandaloneGoLinesAndSkipsBlankBatches()
    {
        const string sqlText = """
            SELECT 1;
            GO

            SELECT 2;
            go
               GO   

            SELECT 3;
            """;

        IReadOnlyList<string> batches = TestAppReplaySqlServerSeedScriptExecutor.SplitBatches(sqlText);

        Assert.Equal(3, batches.Count);
        Assert.Equal("SELECT 1;", batches[0]);
        Assert.Equal("SELECT 2;", batches[1]);
        Assert.Equal("SELECT 3;", batches[2]);
    }

    [Fact]
    public void SplitBatches_DoesNotSplitWhenGoAppearsInsideOtherSqlText()
    {
        const string sqlText = """
            INSERT INTO [dbo].[Messages] ([Text]) VALUES (N'GO');
            SELECT N'go home';
            """;

        IReadOnlyList<string> batches = TestAppReplaySqlServerSeedScriptExecutor.SplitBatches(sqlText);

        var batch = Assert.Single(batches);
        Assert.Contains("VALUES (N'GO')", batch);
        Assert.Contains("SELECT N'go home';", batch);
    }
}
