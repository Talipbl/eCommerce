namespace ECommerce.Core.Domain
{
    /// <summary>İş kuralı ihlali; mesaj kullanıcıya gösterilebilir.</summary>
    public class DomainException(string message) : Exception(message);
}
