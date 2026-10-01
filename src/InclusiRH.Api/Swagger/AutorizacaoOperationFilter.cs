using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InclusiRH.Api.Swagger;

public sealed class AutorizacaoOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (PermiteAnonimo(context.MethodInfo))
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        ];

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Não autenticado." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Sem permissão para este papel." });
    }

    private static bool PermiteAnonimo(MethodInfo metodo) =>
        metodo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
        || metodo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;
}
