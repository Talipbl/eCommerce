using ECommerce.Application.Common;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Infrastructure;

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
