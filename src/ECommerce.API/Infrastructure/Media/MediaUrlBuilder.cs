using ECommerce.Application.Common;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Infrastructure;

public class MediaUrlBuilder(IOptions<MediaOptions> options) : IMediaUrlBuilder
{
    public string ToPublicUrl(string path) =>
        $"{options.Value.PublicBaseUrl.TrimEnd('/')}{MediaOptions.RequestPath}/{path.TrimStart('/')}";
}
