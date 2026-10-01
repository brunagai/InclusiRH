using Microsoft.AspNetCore.Diagnostics;

namespace InclusiRH.Api.Services;

public sealed class RegraDeNegocioExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RegraDeNegocioException regra)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await httpContext.Response.WriteAsJsonAsync(new { erro = regra.Message }, cancellationToken);
        return true;
    }
}
