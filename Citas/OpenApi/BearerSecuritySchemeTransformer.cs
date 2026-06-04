using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Citas.OpenApi;

/// <summary>
/// Declara el esquema de seguridad "Bearer" (JWT) en el documento OpenAPI para que
/// Scalar muestre el botón "Authorize" y adjunte el token en cada petición.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == "Bearer"))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Pega el token JWT obtenido en POST /Auth/token (sin escribir 'Bearer ')."
            }
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null)
                continue;

            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(requirement);
            }
        }
    }
}
