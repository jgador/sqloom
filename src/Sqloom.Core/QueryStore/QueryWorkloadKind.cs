namespace Sqloom.Core.QueryStore;

/// <summary>
/// Represents query workload kind.
/// </summary>
public enum QueryWorkloadKind
{
    /// <summary>
    /// Workload attributed to the application under test.
    /// </summary>
    App,
    /// <summary>
    /// Workload attributed to development or database tooling.
    /// </summary>
    Tooling,
    /// <summary>
    /// Workload attributed to the hosting or database platform.
    /// </summary>
    Platform,
    /// <summary>
    /// Workload attributed to internal database activity.
    /// </summary>
    Internal,
    /// <summary>
    /// Workload whose owner could not be determined.
    /// </summary>
    Unknown,
}
