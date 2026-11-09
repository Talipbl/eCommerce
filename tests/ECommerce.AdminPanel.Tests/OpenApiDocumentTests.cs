using System.Text.Json;

namespace ECommerce.AdminPanel.Tests;

/// <summary>Panel client'ı bu dokümandan üretildiği için cevap şeması eksik uç, gövdesiz <c>Task</c> dönen bir client metodu demektir.</summary>
public class OpenApiDocumentTests
{
    private static JsonElement Paths()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ECommerce.sln")))
            directory = directory.Parent;

        var path = Path.Combine(directory!.FullName, "src", "ECommerce.API", "openapi", "ecommerce-api.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("paths");
    }

    private static IEnumerable<(string Operation, JsonElement Responses)> Operations() =>
        Paths().EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Select(op => ($"{op.Name.ToUpperInvariant()} {path.Name}", op.Value.GetProperty("responses"))));

    [Fact]
    public void Every_operation_declares_a_success_response()
    {
        Operations()
            .Where(op => !op.Responses.EnumerateObject().Any(r => r.Name.StartsWith('2')))
            .Select(op => op.Operation)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Ok_responses_have_a_body_schema()
    {
        Operations()
            .Where(op => op.Responses.TryGetProperty("200", out var ok) && !ok.TryGetProperty("content", out _))
            .Select(op => op.Operation)
            .ShouldBeEmpty();
    }

    [Fact]
    public void File_uploads_are_plain_binary_fields()
    {
        var upload = Paths().GetProperty("/api/v1/catalog/products/{id}/images").GetProperty("post")
            .GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema");

        upload.TryGetProperty("allOf", out _).ShouldBeFalse();
        upload.GetProperty("properties").GetProperty("file").GetProperty("format").GetString().ShouldBe("binary");
    }
}
