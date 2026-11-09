using ECommerce.Application.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Infrastructure;

public class MediaOptions
{
    public const string SectionName = "Media";
    public const string RequestPath = "/media";

    /// <summary>Dosyaların yazılacağı klasör; göreliyse uygulama köküne göre çözülür.</summary>
    public string RootPath { get; set; } = "media";

    /// <summary>Dış dünyaya açık adres (ör. https://api.ornek.com). Trendyol mutlak https görsel adresi ister.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}

public class LocalFileStorage(IOptions<MediaOptions> options, IWebHostEnvironment environment) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));

    public async Task<string> SaveAsync(string directory, string extension, Stream content, CancellationToken cancellationToken)
    {
        var relative = $"{directory.Trim('/')}/{Guid.NewGuid():N}{extension}";
        var fullPath = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, cancellationToken);
        return relative;
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string Root => _root;

    private string Resolve(string relative)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relative));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Geçersiz dosya yolu.");
        return fullPath;
    }
}

public class MediaUrlBuilder(IOptions<MediaOptions> options) : IMediaUrlBuilder
{
    public string ToPublicUrl(string path) =>
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}{MediaOptions.RequestPath}/{path.TrimStart('/')}";
}

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
