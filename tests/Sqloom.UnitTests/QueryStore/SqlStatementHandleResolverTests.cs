using System.Collections.Generic;
using Sqloom.SqlServer.QueryStore;
using Sqloom.QueryStore.QueryStore;
using Xunit;

namespace Sqloom.SqlServer.Tests.QueryStore;

/// <summary>
/// Exercises SQL statement handle resolver.
/// </summary>
public sealed class SqlStatementHandleResolverTests
{
    [Fact]
    public void BuildResolution_UsesFirstNonEmptyStatementHandleAndPreservesCandidates()
    {
        var resolution = SqlStatementHandleResolver.BuildResolution(
            "SELECT 1",
            "SELECT 1",
            new List<SqlStatementHandleResolver.SqlHandleCandidateRecord>
            {
                new("Raw", "Default", 0, null),
                new("ParameterDefinitionPrefix", "None", 0, "0xAAAA"),
                new("ParameterDefinitionPrefix", "Simple", 2, "0xBBBB"),
            });

        Assert.Equal("0xAAAA", resolution.StatementSqlHandle);
        Assert.Equal("SELECT 1", resolution.ComparableSqlText);
        Assert.Collection(
            resolution.Candidates,
            candidate =>
            {
                Assert.Equal("Raw", candidate.QueryTextShape);
                Assert.Equal("Default", candidate.RequestedParamType);
                Assert.Null(candidate.StatementSqlHandle);
            },
            candidate =>
            {
                Assert.Equal("ParameterDefinitionPrefix", candidate.QueryTextShape);
                Assert.Equal("None", candidate.RequestedParamType);
                Assert.Equal("0xAAAA", candidate.StatementSqlHandle);
            },
            candidate =>
            {
                Assert.Equal("ParameterDefinitionPrefix", candidate.QueryTextShape);
                Assert.Equal("Simple", candidate.RequestedParamType);
                Assert.Equal("0xBBBB", candidate.StatementSqlHandle);
            });
    }

    [Fact]
    public void BuildResolution_PreservesResolverErrors()
    {
        var resolution = SqlStatementHandleResolver.BuildResolution(
            "SELECT 1",
            "SELECT 1",
            new List<SqlStatementHandleResolver.SqlHandleCandidateRecord>(),
            "Permission denied.");

        Assert.Null(resolution.StatementSqlHandle);
        Assert.Equal("Permission denied.", resolution.ErrorMessage);
        Assert.Empty(resolution.Candidates);
    }

    [Fact]
    public void BuildQueryTextCandidates_SplitsBatchStatementsAndPrefixesOnlyStatementParameters()
    {
        var candidates =
            SqlStatementHandleResolver.BuildQueryTextCandidates(
                """
                SET NOCOUNT ON;
                SELECT [Id]
                FROM [dbo].[ExpenseRecord]
                WHERE [UserId] = @UserId
                  AND EXISTS (SELECT 1 FROM OPENJSON(@CandidateExpenseIdsJson));
                SELECT [CurrencyCode]
                FROM #UserExpenses
                WHERE [Category] = N'Groceries';
                """,
                [
                    new SqlHandleParameter
                    {
                        Name = "@UserId",
                        DbType = "NVarChar",
                        Size = 450,
                    },
                    new SqlHandleParameter
                    {
                        Name = "@CandidateExpenseIdsJson",
                        DbType = "NVarChar",
                    },
                    new SqlHandleParameter
                    {
                        Name = "@Unused",
                        DbType = "Int",
                    },
                ]);

        Assert.Collection(
            candidates,
            candidate =>
            {
                Assert.Equal("Statement1.Raw", candidate.QueryTextShape);
                Assert.StartsWith("SELECT [Id]", candidate.QuerySqlText);
            },
            candidate =>
            {
                Assert.Equal("Statement1.ParameterDefinitionPrefix", candidate.QueryTextShape);
                Assert.StartsWith(
                    "(@UserId nvarchar(450),@CandidateExpenseIdsJson nvarchar(max))SELECT [Id]",
                    candidate.QuerySqlText);
                Assert.DoesNotContain("@Unused", candidate.QuerySqlText);
            },
            candidate =>
            {
                Assert.Equal("Statement2.Raw", candidate.QueryTextShape);
                Assert.StartsWith("SELECT [CurrencyCode]", candidate.QuerySqlText);
            });
    }
}
