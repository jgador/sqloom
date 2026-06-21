using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sqloom.AspNetCore.Capture;

/// <summary>
/// Registers replay SQL capture middleware for ASP.NET Core apps.
/// </summary>
public sealed class ReplaySqlCaptureStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return applicationBuilder =>
        {
            applicationBuilder.UseMiddleware<ReplaySqlCaptureMiddleware>();
            next(applicationBuilder);
        };
    }
}
