using ECommerce.Application.Common;
using Microsoft.Extensions.FileProviders;

namespace ECommerce.API.Infrastructure;

public static class MediaRegistration
{
    public static IServiceCollection AddMedia(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());
        services.AddSingleton<IMediaUrlBuilder, MediaUrlBuilder>();
        return services;
    }

    public static IApplicationBuilder UseMedia(this IApplicationBuilder app)
    {
        var root = app.ApplicationServices.GetRequiredService<LocalFileStorage>().Root;
        Directory.CreateDirectory(root);

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = MediaOptions.RequestPath,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"
        });
    }
}
