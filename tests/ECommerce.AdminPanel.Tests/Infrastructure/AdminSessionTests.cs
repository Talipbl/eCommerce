using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class AdminSessionTests
{
    private static AdminSession Session() => new("id", "admin", new ApiTokens("a1", DateTimeOffset.UtcNow, "r1"));

    [Fact]
    public async Task Parallel_refreshes_with_same_token_call_api_once()
    {
        var session = Session();
        var auth = FakeAuthClient.Rotating(blocked: true);

        var refreshes = Enumerable.Range(0, 5).Select(_ => session.RefreshAsync("a1", auth)).ToArray();
        auth.Release();
        var tokens = await Task.WhenAll(refreshes);

        auth.RefreshCalls.ShouldBe(1);
        tokens.ShouldAllBe(token => token == "r1-access");
        session.Tokens.RefreshToken.ShouldBe("r1+");
    }

    [Fact]
    public async Task Refresh_with_already_replaced_token_returns_current_token()
    {
        var session = Session();
        var auth = FakeAuthClient.Rotating();
        await session.RefreshAsync("a1", auth);

        var token = await session.RefreshAsync("a1", auth);

        token.ShouldBe("r1-access");
        auth.RefreshCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Rejected_refresh_expires_session_for_all_waiting_requests()
    {
        var session = Session();
        var auth = new FakeAuthClient(_ => null, blocked: true);

        var refreshes = Enumerable.Range(0, 3).Select(_ => session.RefreshAsync("a1", auth)).ToArray();
        auth.Release();

        foreach (var refresh in refreshes)
            await Should.ThrowAsync<AdminSessionExpiredException>(refresh);
        auth.RefreshCalls.ShouldBe(1);
    }
}
