using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECommerce.Persistence;

/// <summary>
/// Tarihler veritabanında UTC saklanır ama datetime2 saat dilimi taşımaz; okunan değer UTC olarak işaretlenir.
/// Böylece API tarihleri "Z" ekiyle yazar ve client'lar yerel saat sanmaz.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
