namespace Sqloom.Host;

/// <summary>
/// Represents the supported Sqloom host command kinds.
/// </summary>
internal enum HostCommandKind
{
    None,
    Help,
    Init,
    Observe,
    Endpoints,
    Tune,
    Replay,
    Correlate,
    Advise,
}
