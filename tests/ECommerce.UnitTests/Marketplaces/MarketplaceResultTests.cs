using ECommerce.Application.Marketplaces;
using Trendyol.Client;

namespace ECommerce.UnitTests.Marketplaces;

public class MarketplaceResultTests
{
    [Theory]
    [InlineData(400, MarketplaceErrorKind.Validation)]
    [InlineData(401, MarketplaceErrorKind.Unauthorized)]
    [InlineData(403, MarketplaceErrorKind.Unauthorized)]
    [InlineData(404, MarketplaceErrorKind.NotFound)]
    [InlineData(409, MarketplaceErrorKind.Conflict)]
    [InlineData(429, MarketplaceErrorKind.RateLimited)]
    [InlineData(500, MarketplaceErrorKind.Unavailable)]
    [InlineData(503, MarketplaceErrorKind.Unavailable)]
    public void Trendyol_status_codes_map_to_error_kinds(int status, MarketplaceErrorKind expected)
    {
        var response = TrendyolResponse<string>.Failure(status, new TrendyolError(null, [new TrendyolErrorDetail("k", "Hata", null)], "{}"));

        var result = response.ToMarketplaceResult();

        result.Succeeded.ShouldBeFalse();
        result.Error!.Kind.ShouldBe(expected);
        result.Error.UpstreamStatus.ShouldBe(status);
        result.Error.Messages.ShouldBe(["Hata"]);
    }

    [Fact]
    public void Successful_response_maps_value()
    {
        var result = TrendyolResponse<string>.Success(200, "ok").ToMarketplaceResult(v => v.ToUpperInvariant());

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe("OK");
    }
}
