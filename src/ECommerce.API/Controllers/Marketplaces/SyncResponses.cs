namespace ECommerce.API.Controllers.Marketplaces;

// Elle tetiklenen senkron işlerinin sayaç cevapları; JSON alan adları önceki anonim cevaplarla aynıdır.
public record ListingSyncResponse(int Batches, int Updated);

public record ProcessedCountResponse(int Processed);

public record RequeuedCountResponse(int Requeued);
