namespace Sqloom.Core.QueryStore;

/// <summary>
/// Represents correlation match kind.
/// </summary>
public enum CorrelationMatchKind
{
    /// <summary>
    /// Replay and Query Store records have an exact statement-handle match.
    /// </summary>
    StatementHandleExact = 0,
    /// <summary>
    /// Replay and Query Store records have an exact normalized-text match.
    /// </summary>
    QueryTextExact = 1,
    /// <summary>
    /// Replay and Query Store records match through a normalized SQL fingerprint.
    /// </summary>
    FingerprintFallback = 2,
    /// <summary>
    /// No Query Store record could be correlated with the replay command.
    /// </summary>
    Unmatched = 3,
}
