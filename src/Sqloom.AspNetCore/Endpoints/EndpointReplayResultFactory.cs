using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Creates replay results from executed requests and captured SQL.
/// </summary>
internal static class EndpointReplayResultFactory
{
    public static EndpointReplayResult CreateFailed(
        EndpointReplayPlanItem planItem,
        string artifactPath,
        string errorMessage)
    {
        return new EndpointReplayResult
        {
            OperationKey = planItem.OperationKey,
            HttpMethod = planItem.HttpMethod,
            Route = planItem.Route,
            Persona = planItem.Persona,
            Status = "failed",
            ErrorMessage = errorMessage,
            Request = new EndpointReplayRequest
            {
                OperationKey = planItem.OperationKey,
                HttpMethod = planItem.HttpMethod,
                Route = planItem.Route,
                Persona = planItem.Persona,
                RelativePathAndQuery = planItem.Route,
            },
            ArtifactPath = artifactPath,
        };
    }
}
