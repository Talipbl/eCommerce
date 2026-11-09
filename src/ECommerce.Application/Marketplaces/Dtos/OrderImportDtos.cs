namespace ECommerce.Application.Marketplaces;

public record OrderImportSummary(string Marketplace, int Imported, int Updated, int Skipped, string? Error = null);
