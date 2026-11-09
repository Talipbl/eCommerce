using ECommerce.Admin;
using ECommerce.Application.Auth;
using ECommerce.Application.Catalog;
using ECommerce.Application.Customers;
using ECommerce.Application.Dashboard;
using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Application.Ordering;
using ECommerce.Application.Shopping;
using ECommerce.Customers;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
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
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services
            .AddAuth(configuration)
            .AddCatalog()
            .AddCustomers()
            .AddOrdering(configuration)
            .AddTrendyolIntegration(configuration)
            .AddMarketplaces()
            .AddDashboard(configuration);
    }

    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>().Bind(configuration.GetSection(AuthOptions.SectionName));
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.HasValidSigningKey, "Jwt:SigningKey en az 32 bayt olmalı ve user-secrets veya ortam değişkeni ile tanımlanmalıdır.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher<DefAdmin>, PasswordHasher<DefAdmin>>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    private static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddScoped<ICategoryAdminService, CategoryAdminService>();
        services.AddScoped<IBrandAdminService, BrandAdminService>();
        services.AddScoped<ProductAdminService>();
        services.AddScoped<IProductAdminService>(sp => sp.GetRequiredService<ProductAdminService>());
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<IStorefrontService, StorefrontService>();
        return services;
    }

    private static IServiceCollection AddCustomers(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
        services.AddScoped<ICustomerAuthService, CustomerAuthService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        return services;
    }

    private static IServiceCollection AddOrdering(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CheckoutOptions>(configuration.GetSection(CheckoutOptions.SectionName));
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IOrderManagementService, OrderManagementService>();
        return services;
    }

    private static IServiceCollection AddTrendyolIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTrendyol(configuration);
        services.AddScoped<ITrendyolCatalogService, TrendyolCatalogService>();
        services.AddScoped<TrendyolBatchTracker>();
        services.AddScoped<ITrendyolProductService, TrendyolProductService>();
        services.AddScoped<ITrendyolBatchService, TrendyolBatchService>();
        services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
        services.AddScoped<ITrendyolListingService, TrendyolListingService>();
        services.AddScoped<IMarketplaceProvider, TrendyolMarketplaceProvider>();
        return services;
    }

    private static IServiceCollection AddMarketplaces(this IServiceCollection services)
    {
        services.AddScoped<IMarketplaceInventorySyncService, MarketplaceInventorySyncService>();
        services.AddScoped<IMarketplaceOrderImportService, MarketplaceOrderImportService>();
        services.AddScoped<IMarketplaceOrderStatusSyncService, MarketplaceOrderStatusSyncService>();
        services.AddScoped<IMarketplaceSyncAdminService, MarketplaceSyncAdminService>();
        return services;
    }

    private static IServiceCollection AddDashboard(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DashboardOptions>()
            .Bind(configuration.GetSection(DashboardOptions.SectionName))
            .Validate(o => o.LowStockThreshold >= 0 && o.LowStockListSize is > 0 and <= 200, "Dashboard:LowStockThreshold negatif olamaz, LowStockListSize 1-200 arasında olmalı.")
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _), "Dashboard:TimeZone geçerli bir saat dilimi değil.")
            .ValidateOnStart();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
