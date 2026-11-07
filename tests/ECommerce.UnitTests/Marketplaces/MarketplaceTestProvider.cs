using ECommerce.Application.Marketplaces;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using NSubstitute;

namespace ECommerce.UnitTests.Marketplaces;

/// <summary>Senkron servislerini pazaryerinden bağımsız test etmek için Trendyol sınırlarına sahip sahte sağlayıcı.</summary>
public static class MarketplaceTestProvider
{
    public static IMarketplaceProvider Create()
    {
        var provider = Substitute.For<IMarketplaceProvider>();
        provider.Name.Returns(MarketplaceNames.Trendyol);
        provider.Channel.Returns(OrderChannel.Trendyol);
        provider.OrderNumberPrefix.Returns("TY");
        provider.MaxOrderWindow.Returns(TimeSpan.FromDays(14));
        provider.MaxOrderLookback.Returns(TimeSpan.FromDays(28));
        provider.MaxOrderPages.Returns(50);
        provider.MaxPriceInventoryItems.Returns(1000);
        provider.PushPriceInventoryAsync(default!, default).ReturnsForAnyArgs(MarketplaceResult<bool>.Success(true));
        provider.UpdateOrderStatusAsync(default!, default).ReturnsForAnyArgs(MarketplaceResult<bool>.Success(true));
        return provider;
    }

    public static MarketplaceResult<T> Failure<T>(MarketplaceErrorKind kind, params string[] messages) =>
        MarketplaceResult<T>.Failure(new MarketplaceError(kind, null, messages));
}
