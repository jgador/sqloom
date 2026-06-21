using Sqloom.Core.Execution;

namespace Sqloom.Core.Contracts;

/// <summary>
/// Supplies the application-specific replay integration that the generic Sqloom host composes.
/// </summary>
public interface IAppIntegration
{
    string AppName { get; }

    ReplayProfile GetReplayProfile();

    IReplayHostFactory CreateReplayHostFactory();
}
