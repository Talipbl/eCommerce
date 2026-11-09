namespace ECommerce.AdminPanel.Api;

// Üretilen client tanımlı hata cevaplarında (400 ProblemDetails, 404...) gövdeyi varsayılan olarak saklamaz;
// ApiProblem alan hatalarını ve upstreamStatus'u bu metinden okur. Yeni kaydedilen her client buraya eklenir.
public partial class AuthClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class DashboardClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class CategoriesClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class BrandsClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class ProductsClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class OrdersClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class MarketplaceSyncClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class TrendyolCatalogClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class TrendyolIntegrationClient { partial void Initialize() => ReadResponseAsString = true; }
public partial class TrendyolBatchesClient { partial void Initialize() => ReadResponseAsString = true; }
