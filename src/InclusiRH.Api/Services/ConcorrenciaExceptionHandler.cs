using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Services;

/// <summary>
/// Dois usuários (ou o worker de alertas) alteraram o mesmo registro ao mesmo
/// tempo. A segunda gravação é recusada e quem a enviou recebe 409 para recarregar.
/// </summary>
public sealed class ConcorrenciaExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new { erro = "Este registro foi alterado enquanto você trabalhava nele. Recarregue e tente de novo." },
            cancellationToken);
        return true;
    }
}
