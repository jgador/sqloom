using System.Collections.Generic;
using Sqloom.Pipeline.Execution;

namespace Sqloom.Host;

/// <summary>
/// Carries cleaned SQL proposals together with non-fatal issues found during normalization.
/// </summary>
internal sealed class NormalizedProposalResult
{
    public required IReadOnlyList<SqlTuningProposal> Proposals { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}
