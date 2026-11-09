using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

/// <summary>Yenilemeleri sayan sahte auth client. <c>blocked</c> ise yenileme cevabı <see cref="Release"/> çağrılana kadar bekler.</summary>
internal sealed class FakeAuthClient(Func<string, ApiTokens?> refresh, bool blocked = false) : IApiAuthClient
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _refreshCalls;

    public int RefreshCalls => Volatile.Read(ref _refreshCalls);

    /// <summary>Her yenilemede refresh token'ı döndürür: "r" → access "r-access", refresh "r+".</summary>
    public static FakeAuthClient Rotating(bool blocked = false) =>
        new(old => new ApiTokens($"{old}-access", DateTimeOffset.UtcNow.AddMinutes(15), $"{old}+"), blocked);

    public void Release() => _gate.TrySetResult();

    public Task<ApiTokens> LoginAsync(string userName, string password, CancellationToken cancellationToken) => throw new NotSupportedException();

    public async Task<ApiTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _refreshCalls);
        if (blocked)
            await _gate.Task;
        return refresh(refreshToken);
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) => Task.CompletedTask;
}
