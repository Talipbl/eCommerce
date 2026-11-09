namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

public class OrderPackagesFilter
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Status { get; set; }
    public string? OrderNumber { get; set; }
    public long? ShipmentPackageId { get; set; }
    public int Page { get; set; }
    public int Size { get; set; } = 50;
}
