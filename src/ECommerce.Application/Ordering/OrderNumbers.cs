using System.Security.Cryptography;

namespace ECommerce.Application.Ordering;

public static class OrderNumbers
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Okunması kolay, tahmin edilmesi zor sipariş numarası (ör. EC261107K7M2QX).</summary>
    public static string New(DateTime now) =>
        "EC" + now.ToString("yyMMdd") + RandomNumberGenerator.GetString(Alphabet, 6);
}
