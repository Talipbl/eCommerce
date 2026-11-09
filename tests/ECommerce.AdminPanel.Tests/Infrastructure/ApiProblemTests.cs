using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class ApiProblemTests
{
    internal static ApiException Exception(int status, string? body, Dictionary<string, IEnumerable<string>>? headers = null) =>
        new("API hatası", status, body, headers ?? [], null);

    [Fact]
    public void Validation_problem_is_read_per_field()
    {
        var body = """
            {"title":"One or more validation errors occurred.","status":400,
             "errors":{"Name":["Ad zorunludur."],"Variants[0].Barcode":["Barkod geçersiz.","Barkod tekrar ediyor."]}}
            """;

        var problem = ApiProblem.From(Exception(400, body));

        problem.Status.ShouldBe(400);
        problem.FieldErrors["Name"].ShouldBe(["Ad zorunludur."]);
        problem.FieldErrors["Variants[0].Barcode"].ShouldBe(["Barkod geçersiz.", "Barkod tekrar ediyor."]);
        problem.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void Marketplace_problem_reads_messages_upstream_status_and_retry_after()
    {
        var body = """{"title":"Pazaryeri istek limiti aşıldı.","status":503,"errors":["Too many requests"],"upstreamStatus":429}""";
        var headers = new Dictionary<string, IEnumerable<string>> { ["Retry-After"] = ["60"] };

        var problem = ApiProblem.From(Exception(503, body, headers));

        problem.Title.ShouldBe("Pazaryeri istek limiti aşıldı.");
        problem.Messages.ShouldBe(["Too many requests"]);
        problem.FieldErrors.ShouldBeEmpty();
        problem.UpstreamStatus.ShouldBe(429);
        problem.RetryAfter.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("[1,2]")]
    public void Non_problem_body_falls_back_to_generic_title(string? body)
    {
        var problem = ApiProblem.From(Exception(502, body));

        problem.Status.ShouldBe(502);
        problem.Title.ShouldBe("İşlem tamamlanamadı.");
        problem.FieldErrors.ShouldBeEmpty();
        problem.Messages.ShouldBeEmpty();
    }
}
