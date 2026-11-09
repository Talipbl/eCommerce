namespace ECommerce.Application.Common;

public interface IFileStorage
{
    /// <summary>Dosyayı kaydeder ve depolama köküne göreli yolunu döner.</summary>
    Task<string> SaveAsync(string directory, string extension, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
