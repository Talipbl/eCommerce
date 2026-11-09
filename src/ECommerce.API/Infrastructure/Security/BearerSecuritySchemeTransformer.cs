using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ECommerce.API.Infrastructure;

/// <summary>
/// OpenAPI dokümanına JWT Bearer şemasını ekler; Scalar arayüzünden token girilerek korumalı uçlar denenebilir.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "POST /api/v1/auth/login cevabındaki accessToken değeri."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}
