using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

public enum OutboxStatus
{
    Pending,
    Succeeded,
    Failed
}

/// <summary>
/// Pazaryerine gönderilecek kaydın ortak tekrar deneme kuralları: geçici hatada artan bekleme, sınır aşılınca kalıcı hata.
/// </summary>
public abstract class MarketplaceOutboxMessage : AggregateRoot<int>
{
    public const int MaxAttempts = 5;

    public OutboxStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? NextAttemptAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    public bool IsDue(DateTime now) => Status == OutboxStatus.Pending && (NextAttemptAt is null || NextAttemptAt <= now);

    public void MarkSucceeded(DateTime now)
    {
        Status = OutboxStatus.Succeeded;
        ProcessedAt = now;
        NextAttemptAt = null;
        Error = null;
    }

    public void MarkFailed(string error, DateTime now)
    {
        Status = OutboxStatus.Failed;
        ProcessedAt = now;
        NextAttemptAt = null;
        Error = Truncate(error);
    }

    public void MarkTransientFailure(string error, DateTime now)
    {
        Attempts++;
        if (Attempts >= MaxAttempts)
        {
            MarkFailed(error, now);
            return;
        }

        Error = Truncate(error);
        NextAttemptAt = now.AddMinutes(Math.Pow(2, Attempts - 1));
    }

    public void Requeue()
    {
        if (Status != OutboxStatus.Failed)
            throw new DomainException("Yalnızca hatalı kayıtlar tekrar denenebilir.");

        Status = OutboxStatus.Pending;
        Attempts = 0;
        NextAttemptAt = null;
        ProcessedAt = null;
        Error = null;
    }

    private static string Truncate(string error) => error.Length <= 2000 ? error : error[..2000];
}
