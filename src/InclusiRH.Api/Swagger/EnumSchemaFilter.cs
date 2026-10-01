using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InclusiRH.Api.Swagger;

public sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }

        schema.Enum ??= new List<IOpenApiAny>();
        schema.Enum.Clear();
        schema.Type = "string";
        schema.Format = null;

        foreach (var nome in Enum.GetNames(context.Type))
        {
            schema.Enum.Add(new OpenApiString(nome));
        }
    }
}
