namespace ECommerce.Core.Persistence
{
    /// <summary>Kayıt okunduktan sonra başka bir işlem tarafından değiştirildi (iyimser eşzamanlılık).</summary>
    public class ConcurrencyConflictException(Exception inner)
        : Exception("Kayıt başka bir işlem tarafından değiştirildi; lütfen tekrar deneyin.", inner);
}
