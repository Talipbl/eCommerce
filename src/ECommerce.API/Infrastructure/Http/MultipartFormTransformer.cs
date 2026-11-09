using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ECommerce.API.Infrastructure;

/// <summary>
/// Form dosyası alan uçların şemasını client üreticilerin anladığı biçime çevirir: <c>allOf</c> parçaları tek nesneye birleşir,
/// <c>IFormFile</c> referansı <c>string/binary</c> olur. Aksi halde NSwag dosya parametresini üretmiyor.
/// </summary>
internal sealed class MultipartFormTransformer : IOpenApiOperationTransformer
{
    private const string FormFileSchemaId = "IFormFile";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.RequestBody?.Content?.TryGetValue("multipart/form-data", out var media) != true || media!.Schema is not { AllOf.Count: > 0 } schema)
            return Task.CompletedTask;

        var merged = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = new Dictionary<string, IOpenApiSchema>(), Required = new HashSet<string>() };
        foreach (var part in schema.AllOf)
        {
            foreach (var (name, property) in part.Properties ?? new Dictionary<string, IOpenApiSchema>())
            {
                var isFile = property is OpenApiSchemaReference { Reference.Id: FormFileSchemaId };
                merged.Properties[name] = isFile ? new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } : property;
                if (isFile)
                    merged.Required.Add(name);
            }
        }

        media.Schema = merged;
        return Task.CompletedTask;
    }

    /// <summary>Birleştirmeden sonra kullanılmayan <c>IFormFile</c> şemasını dokümandan kaldırır.</summary>
    public static Task RemoveFormFileSchema(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components?.Schemas?.Remove(FormFileSchemaId);
        return Task.CompletedTask;
    }
}
