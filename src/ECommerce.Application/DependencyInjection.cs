using ECommerce.Application.Catalog;
using ECommerce.Application.Customers;
using ECommerce.Application.Ordering;
using ECommerce.Application.Shopping;
using ECommerce.Customers;
using Microsoft.AspNetCore.Identity;
using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trendyol.Client;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMappings();
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<CreateTrendyolProductsRequestValidator>();

        services.AddScoped<ICategoryAdminService, CategoryAdminService>();
        services.AddScoped<IBrandAdminService, BrandAdminService>();
        services.AddScoped<ProductAdminService>();
        services.AddScoped<IProductAdminService>(sp => sp.GetRequiredService<ProductAdminService>());
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IStorefrontService, StorefrontService>();

        services.Configure<CheckoutOptions>(configuration.GetSection(CheckoutOptions.SectionName));
        services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
        services.AddScoped<ICustomerAuthService, CustomerAuthService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IOrderManagementService, OrderManagementService>();

        services.AddTrendyol(configuration);
        services.AddScoped<ITrendyolCatalogService, TrendyolCatalogService>();
        services.AddScoped<TrendyolBatchTracker>();
        services.AddScoped<ITrendyolProductService, TrendyolProductService>();
        services.AddScoped<ITrendyolBatchService, TrendyolBatchService>();
        services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
        services.AddScoped<ITrendyolListingService, TrendyolListingService>();
        services.AddScoped<IMarketplaceProvider, TrendyolMarketplaceProvider>();

        services.AddScoped<IMarketplaceInventorySyncService, MarketplaceInventorySyncService>();
        services.AddScoped<IMarketplaceOrderImportService, MarketplaceOrderImportService>();
        services.AddScoped<IMarketplaceOrderStatusSyncService, MarketplaceOrderStatusSyncService>();
        services.AddScoped<IMarketplaceSyncAdminService, MarketplaceSyncAdminService>();

        return services;
    }
}
