using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class ForwardedForHandlerTests
{
    [Fact]
    public async Task Client_ip_replaces_any_forwarded_header_from_the_browser()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.7");
        var api = new FakeApi().On("GET", "/api/v1/orders", _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var invoker = new HttpMessageInvoker(new ForwardedForHandler(new HttpContextAccessor { HttpContext = httpContext }) { InnerHandler = api });
        var request = new HttpRequestMessage(HttpMethod.Get, "http://api.test/api/v1/orders");
        request.Headers.Add(ForwardedForHandler.HeaderName, "10.0.0.1");

        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        api.Requests.Single().ForwardedFor.ShouldBe("203.0.113.7");
    }
}
