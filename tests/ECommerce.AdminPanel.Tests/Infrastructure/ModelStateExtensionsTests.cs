using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static ECommerce.AdminPanel.Tests.Infrastructure.ApiProblemTests;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class ModelStateExtensionsTests
{
    private static string[] Errors(ModelStateDictionary modelState, string key) =>
        modelState[key]?.Errors.Select(e => e.ErrorMessage).ToArray() ?? [];

    [Fact]
    public void Field_errors_go_to_matching_fields_without_summary()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiProblem(Exception(400, """{"title":"Doğrulama hatası","errors":{"Name":["Ad zorunludur."]}}"""));

        Errors(modelState, "Name").ShouldBe(["Ad zorunludur."]);
        Errors(modelState, string.Empty).ShouldBeEmpty();
    }

    [Fact]
    public void Prefix_is_added_when_form_model_wraps_the_request()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiProblem(Exception(400, """{"errors":{"Name":["Ad zorunludur."]}}"""), prefix: "Request");

        Errors(modelState, "Request.Name").ShouldBe(["Ad zorunludur."]);
    }

    [Fact]
    public void Business_rule_error_becomes_summary_with_detail()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiProblem(Exception(400, """{"title":"İşlem yapılamadı.","detail":"Alt kategorisi olan kategori silinemez."}"""));

        Errors(modelState, string.Empty).ShouldBe(["İşlem yapılamadı. Alt kategorisi olan kategori silinemez."]);
    }

    [Fact]
    public void Conflict_shows_the_business_rule_from_api()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiProblem(Exception(409, """{"title":"Alt kategorisi olan kategori silinemez.","status":409}"""));

        Errors(modelState, string.Empty).ShouldBe(["Alt kategorisi olan kategori silinemez."]);
    }

    [Fact]
    public void Marketplace_error_shows_upstream_status_and_messages()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiProblem(Exception(400, """{"title":"İstek pazaryeri tarafından reddedildi.","errors":["Barkod daha önce kullanılmış."],"upstreamStatus":400}"""));

        Errors(modelState, string.Empty).ShouldBe([
            "İstek pazaryeri tarafından reddedildi. (pazaryeri cevabı: 400)",
            "Barkod daha önce kullanılmış."
        ]);
    }

    [Fact]
    public void Rate_limited_marketplace_error_shows_wait_time()
    {
        var modelState = new ModelStateDictionary();
        var headers = new Dictionary<string, IEnumerable<string>> { ["Retry-After"] = ["60"] };

        modelState.AddApiProblem(Exception(503, """{"title":"Pazaryeri istek limiti aşıldı.","upstreamStatus":429}""", headers));

        Errors(modelState, string.Empty).ShouldBe(["Pazaryeri istek limiti aşıldı. Yaklaşık 60 saniye sonra tekrar deneyin."]);
    }
}
