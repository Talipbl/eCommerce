namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

public class ProductListFilter
{
    public string? Barcode { get; set; }
    public string? StockCode { get; set; }
    public string? ProductMainId { get; set; }
    public long[]? BrandIds { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? DateQueryType { get; set; }
    public int Page { get; set; }
    public int Size { get; set; } = 50;
    public string? NextPageToken { get; set; }
}
