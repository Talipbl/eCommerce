# ECommerce — Yapı Düzenleme Planı

> Tarih: 2026-11-09 · Durum: Kararlar alındı, uygulamaya hazır

## Amaç ve sınırlar

DTO, servis, interface ve validator'ların aynı dosyalarda iç içe durmasını gidermek; dosya ve klasör düzenini **tahmin edilebilir** hale getirmek.

**Basitliği korumak için yapılmayacaklar**
- Yeni proje/katman eklenmez (Infrastructure, Contracts vb. yok). Mevcut 5 proje aynen kalır.
- CQRS/MediatR, generic servis tabanı, AutoMapper benzeri yeni soyutlama eklenmez.
- Davranış değişmez: her faz yalnızca **dosya taşıma / bölme / yeniden adlandırma**dır. Kabul ölçütü: `dotnet build` uyarısız, tüm testler yeşil, OpenAPI uçları aynı.
- Namespace'ler **özellik (feature) seviyesinde** kalır (`ECommerce.Application.Catalog`). `Dtos/` ve `Validators/` alt klasörleri yeni namespace açmaz; böylece `using` değişikliği neredeyse sıfır olur.

---

## Mevcut durumdaki sorunlar

### Application
| Dosya | İçinde olanlar |
|---|---|
| `Customers/CustomerServices.cs` (195 satır) | 5 DTO + 2 interface + 2 servis + 3 validator |
| `Shopping/CartService.cs` | 4 DTO + interface + `PurchasableVariant` + servis + 2 validator |
| `Ordering/OrderDtos.cs` | `CheckoutOptions`, `BankTransferOptions`, 11 DTO, `OrderMapping`, `OrderNumbers` |
| `Ordering/CheckoutService.cs`, `OrderManagementService.cs` | interface + servis + validator'lar |
| `Catalog/CatalogDtos.cs` | DTO'lar arasında `IMediaUrlBuilder` interface'i |
| `Catalog/ProductImageService.cs` | `IFileStorage` + `IProductImageService` + servis |
| `Marketplaces/IMarketplaceProvider.cs` | 8 record + interface + `MarketplaceProviders` |
| `Marketplaces/MarketplaceSyncAdminService.cs` | `OutboxKind` enum + 4 DTO + interface + servis |
| `Marketplaces/MarketplaceInventorySyncService.cs` | interface + servis + `SyncStateRepository` yardımcısı |
| `Marketplaces/Trendyol/TrendyolListingService.cs` (340 satır) | 5 DTO + interface + servis + `PublishListingRequestValidator` |
| `Marketplaces/Trendyol/TrendyolOrderService.cs` | 6 DTO/enum + interface + servis + 4 validator + kural sınıfı |
| `Marketplaces/Trendyol/TrendyolCatalogService.cs` | 5 DTO + interface + servis |

Diğer:
- **İsim çakışması:** `BrandDto` hem `Catalog` hem `Marketplaces.Trendyol` namespace'inde var (OpenAPI şemasında da karışıyor).
- **Kullanılmayan Mapster:** `MappingRegistration` kayıtlı, ama projede hiç `IRegister`/`Adapt` yok. `ServiceRegistrations/` klasörü yalnızca bunu tutuyor.
- **DI dağınık:** `AddApplication` düz bir liste; auth kaydı ayrı dosyada (`Auth/AuthServiceCollectionExtensions.cs`), API'de ayrıca çağrılıyor.

### API
- `Controllers/Catalog/CatalogAdminControllers.cs`: 3 controller + `ReorderImagesRequest`.
- `Controllers/Customers/CustomerControllers.cs`: hesap, **sepet** ve **sipariş** controller'ları tek dosyada, sepet/sipariş yanlış klasörde.
- `TrendyolOrdersController.cs` / `TrendyolProductsController.cs` içinde filtre sınıfları.
- `Infrastructure/` 11 dosya düz: güvenlik, medya (Application port implementasyonları), worker'lar, seed, HTTP sonuç dönüştürücüleri bir arada.

### Domain
- Gereksiz `Domains/` ara klasörü; klasör `ECommerce.Domain/Domains/Catalog`, namespace `ECommerce.Catalog`.
- Ayrı aggregate'ler aynı dosyada: `Category.cs` içinde `Brand`; `MarketplaceListing.cs` içinde kategori/marka eşlemeleri ve `MarketplaceNames`; `MarketplaceSync.cs` içinde 4 ayrı tip.

---

## Hedef kurallar (tek sayfalık)

1. **Bir servis = iki dosya:** `IXxxService.cs` ve `XxxService.cs`, özellik klasörünün kökünde.
2. **DTO'lar** `<Özellik>/Dtos/` altında, servis başına bir dosya: `CategoryDtos.cs` (o servisin request + response record'ları).
3. **Validator'lar** `<Özellik>/Validators/` altında, DTO dosyasıyla eşleşen adla: `CategoryValidators.cs`.
4. **Options** sınıfları kendi dosyasında: `CheckoutOptions.cs`.
5. **Dış dünya portları** (`IFileStorage`, `IMediaUrlBuilder`) `Common/Abstractions/` altında.
6. **Yardımcılar** (`OrderMapping`, `OrderNumbers`, `SyncStateRepository`) kullanıldıkları özelliğin kökünde, kendi dosyasında.
7. **API:** dosya başına bir controller; controller klasörleri Application özellikleriyle aynı adı taşır.
8. **Domain:** aggregate root başına bir dosya; yalnızca o köke ait child entity ve value object'ler aynı dosyada kalabilir.

### Örnek: Catalog sonrası
```
Application/Catalog/
  Dtos/
    CategoryDtos.cs        SaveCategoryRequest, CategoryDto, CategoryTreeDto
    BrandDtos.cs           SaveBrandRequest, BrandDto
    ProductDtos.cs         CreateProductRequest, ProductDetailDto, VariantDto, ...
    StorefrontDtos.cs      StoreProductQuery, StoreProductCard, ...
  Validators/
    CatalogValidationRules.cs
    CategoryValidators.cs
    BrandValidators.cs
    ProductValidators.cs
  ICategoryAdminService.cs   CategoryAdminService.cs
  IBrandAdminService.cs      BrandAdminService.cs
  IProductAdminService.cs    ProductAdminService.cs
  IProductImageService.cs    ProductImageService.cs
  IStorefrontService.cs      StorefrontService.cs
Application/Common/
  Abstractions/IFileStorage.cs, IMediaUrlBuilder.cs
  Results.cs
```

---

## Fazlar

Her faz ayrı branch + PR; testler her adımda yeşil kalır. Sıra, en çok karışıklığın olduğu yerden başlar.

### Faz 1 — Application dosya ayrıştırma (en büyük kazanç)
- [x] 1.1 Catalog: `CatalogDtos.cs` bölünür, interface'ler ayrılır, `IMediaUrlBuilder`/`IFileStorage` → `Common/Abstractions`.
- [x] 1.2 Customers: `CustomerServices.cs` → `Dtos/CustomerDtos.cs`, `Dtos/AddressDtos.cs`, 2 interface, 2 servis, `Validators/CustomerValidators.cs`.
- [x] 1.3 Shopping: `CartService.cs` bölünür; `PurchasableVariant` kendi dosyasına.
- [x] 1.4 Ordering: `OrderDtos.cs` → `CheckoutOptions.cs`, `Dtos/CheckoutDtos.cs`, `Dtos/OrderDtos.cs`, `OrderMapping.cs`, `OrderNumbers.cs`; validator'lar `Validators/`.
- [x] 1.5 Auth: `AuthModels.cs` → `Dtos/AuthDtos.cs` (`AuthResult`/`AuthError` dahil); `IAuthService`, `ITokenService` ayrı dosyaya; `AuthValidators.cs` → `Validators/`.
- [x] 1.6 Marketplaces (genel): `IMarketplaceProvider.cs` → interface + `Dtos/MarketplaceProviderDtos.cs` + `MarketplaceProviders.cs`; sync servislerinin interface/DTO'ları ayrılır; `OutboxKind` DTO dosyasına; `SyncStateRepository` kendi dosyasına.
- [x] 1.7 Marketplaces/Trendyol: catalog, listing, order, batch servisleri aynı kalıpla bölünür; `TrendyolProductRequests.cs` → `Dtos/`, validator'lar → `Validators/`.
- [x] 1.8 `Marketplaces.Trendyol.BrandDto` → `TrendyolBrandDto` olarak yeniden adlandırılır (OpenAPI şema adı değişir; front-end henüz yok, risk yok).
- [x] 1.9 DI: `DependencyInjection.cs` tek dosya kalır ama özellik başına private metotlara bölünür (`AddCatalog()`, `AddCustomers()`, `AddOrdering()`, `AddMarketplaces()`, `AddTrendyolIntegration()`). Auth kaydı da bu dosyaya `AddAuth()` olarak taşınır; `Program.cs` sade kalır.
- [x] 1.10 Mapster kalır: `ServiceRegistrations/MappingRegistration.cs` → `Common/Mapping/MappingRegistration.cs` (namespace aynı), `ServiceRegistrations/` klasörü kaldırılır. Eşlemelerin Mapster'a geçişi Faz 5'te.

**Kabul:** Her `.cs` dosyası tek bir role sahip (interface / servis / DTO grubu / validator grubu / options / yardımcı). 300+ satırlık karma dosya kalmaz.

### Faz 2 — API düzeni
- [x] 2.1 Dosya başına bir controller: `CategoriesController.cs`, `BrandsController.cs`, `ProductsController.cs`; `ReorderImagesRequest` → Application `Catalog/Dtos/ProductDtos.cs`. `AuthController` → `Controllers/Auth/`, `AccessTokenResponse` kendi dosyasına.
- [x] 2.2 `CustomerControllers.cs` bölünür: `Customers/CustomerAccountController.cs`, `Shopping/CartController.cs`, `Ordering/CustomerOrdersController.cs`. Route'lar değişmez.
- [x] 2.3 `Controllers/Trendyol/` → `Controllers/Marketplaces/Trendyol/`; `OrderPackagesFilter`, `ProductListFilter` ayrı dosyalara.
- [x] 2.4 `Infrastructure/` alt klasörlere ayrılır:
  ```
  Infrastructure/
    Http/        ResultActionResults, MarketplaceActionResults, ValidationFilter,
                 GlobalExceptionHandler, ClaimsPrincipalExtensions
    Security/    SecurityRegistration, SecurityHeadersMiddleware, BearerSecuritySchemeTransformer
    Media/       MediaOptions, LocalFileStorage, MediaUrlBuilder, MediaRegistration
    Seed/        AdminSeedOptions, AdminSeedHostedService
  BackgroundJobs/  PeriodicTrendyolWorker + 4 worker (her biri kendi dosyasında)
  ```
  `Program.cs`'teki 4 `AddHostedService` satırı `AddBackgroundJobs()` extension'ına alınır.

### Faz 3 — Domain düzeni
- [x] 3.1 `Domains/` ara klasörü kaldırılır (`ECommerce.Domain/Catalog/...`). Namespace'ler **değişmez**; EF snapshot ve migration'lar etkilenmez.
- [x] 3.2 Ayrı aggregate'ler bölünür: `Brand.cs`; `MarketplaceCategoryMapping.cs`, `MarketplaceBrandMapping.cs`, `MarketplaceNames.cs`; `MarketplaceOutboxMessage.cs`, `MarketplaceOrderStatusUpdate.cs`, `MarketplaceSyncState.cs`, `MarketplaceSyncLog.cs`. Ek olarak `CustomerRoles.cs` ve `CustomerRefreshToken.cs` (`AdminRoles`/`AdminRefreshToken` ile tutarlı).
- [x] 3.3 `Admin/Const/AdminRoles.cs` → `Admin/AdminRoles.cs` (tek dosyalık klasör kaldırılır).

### Faz 4 — Testler ve kurallar
- [x] 4.1 `UnitTests/Marketplaces/` (16 dosya) → `Marketplaces/` + `Marketplaces/Trendyol/`; test klasörleri Application ile aynalanır.
- [x] 4.2 `.editorconfig`'e `dotnet_style_namespace_match_folder = false` eklenir (genel); IDE uyarısı çıkmaz.
- [x] 4.3 Hedef kurallar kısa bir `docs/CONVENTIONS.md` dosyasına yazılır, yeni kod bu düzene uyar.

### Faz 5 — Elle eşlemelerin Mapster'a geçişi (sonraki aşama)

Faz 1–4 bittikten sonra, ayrı branch'te yapılır. Bugün Application'da Mapster kayıtlı ama kullanılmıyor; tüm eşlemeler `new XxxDto(...)`, `ToDto(...)` ve `Select(...)` ile elle yazılmış.

**Kurallar**
- Eşlemeler özellik başına `<Özellik>/Mappings/<Özellik>MappingConfig.cs` dosyasında, `IRegister` ile tanımlanır. `MappingRegistration.CreateConfig()` bunları assembly taramasıyla zaten bulur.
- Servisler `IMapper` enjekte eder; global `TypeAdapterConfig.GlobalSettings` ve statik `.Adapt()` kullanılmaz (testte config tek yerden gelsin).
- EF sorgularında projection: `ListAsync(q => q.ProjectToType<XxxDto>(mapper.Config))`. Böylece SQL'de yalnızca gereken kolonlar seçilir, bugünkü `Select(new ...)` davranışı korunur.
- Config `RequireExplicitMapping` + `RequireDestinationMemberSource` ile kurulur; eksik ya da yanlış adlı alan `MappingConfigurationTests`'te derleme anında yakalanır.
- Eşleme yalnızca **veri kopyalama** içindir. Hesaplama, iş kuralı ya da dış servis gerektiren dönüşümler Mapster'a taşınmaz (aşağıdaki "elle kalır" listesi).

**Envanter**

| Grup | Yer | Geçiş |
|---|---|---|
| Basit entity → DTO | `BrandDto`, `CategoryDto` (Catalog); `CustomerProfileDto`, `AddressDto` (Customers); `VariantAttributeDto` | `IRegister` + `mapper.Map<T>()` |
| EF projection | `BrandAdminService.ListAsync`, `MarketplaceSyncAdminService` (`SyncLogDto`, `SyncStateDto`), `TrendyolListingService` (`BrandMappingDto`) | `ProjectToType<T>` |
| Liste/detay DTO'ları | `OrderSummaryDto`, `OrderLineDto`, `OrderStatusChangeDto`, `OrderAddressDto`, `BatchDto`, `CategoryMappingDto`, `ListingDto` | `IRegister`; özel alanlar `.Map(...)` ile |
| Trendyol client → DTO | `TrendyolBrand` → `TrendyolBrandDto`, `AttributeValueDto`, `CategoryAttributeDto`, `CategoryNodeDto` (iç içe ağaç) | `IRegister`; `ToMarketplaceResult(mapper.Map<T>)` |
| Bağlam isteyen | `ProductImageDto` (medya URL'i), `ProductDetailDto`, `OrderDto` (`CheckoutOptions` ile banka bilgisi) | Mapster'ın `MapContext` parametresiyle ya da `AfterMapping`; deneme sonucu karmaşıksa elle kalır |

**Elle kalır (Mapster'a taşınmaz)**
- Düz listeden kurulan ağaçlar: `CategoryTreeDto`, `StoreCategoryDto` (`BuildTree`).
- Hesaplanan değerler: `CartItemDto`/`CartDto` (stok, satılabilirlik, toplam), `StoreProductCard` (marka sözlüğü, min fiyat), `CheckoutPreviewDto`.
- Polimorfik `OutboxEntryDto` (`switch`); iş kuralı içeren dönüşümler: `TrendyolMarketplaceProvider.ToOrder`, `TrendyolListingService`'teki `CreateProductItem` kurulumu, stok sınırlayan `PriceInventoryItem`.
- Domain nesnesi oluşturma (`new VariantAttribute`, `new PackageLine`) ve sonuç/özet record'ları (`OrderImportSummary`, `BatchAcceptedDto`): bunlar eşleme değil.

**Adımlar**
- [x] 5.1 Config kuralları (`RequireExplicitMapping` vb.) açılır; `tests/ECommerce.UnitTests` içine `TestMapper` yardımcısı (`MappingRegistration.CreateConfig()` + `Mapper`) eklenir. Servisi elle kuran testler (`CustomerServiceTests`, `TrendyolBatchServiceTests`, `TrendyolCatalogServiceTests`) bunu kullanır.
- [x] 5.2 Catalog + Customers: basit eşlemeler ve `BrandAdminService` projection'ı.
- [x] 5.3 Ordering: `OrderMapping` statik sınıfı `OrderingMappingConfig`'e dönüşür; `OrderDto` için bağlam denemesi.
- [x] 5.4 Marketplaces: sync admin projection'ları, listing/batch DTO'ları, Trendyol client → DTO eşlemeleri.
- [x] 5.5 Her eşleme için kısa birim testi (özellikle `.Map(...)` ile özelleşen alanlar ve iç içe ağaç).

**Uygulama notları (2026-11-09)**
- `CategoryNodeDto` özyinelemeli ağaç olduğu için Mapster derlemesi sonsuz döngüye giriyor; diğer ağaçlar gibi elle kaldı.
- `ProductDetailDto` görsel URL'i `MappingParameters.Media` çalışma zamanı parametresiyle verilir: `mapper.From(p).AddParameters(...)`.
- `OrderDto.BankTransfer` eşlemede `Ignore` edilir, `OrderMapping.ToOrderDto` ayarlardan `with` ile doldurur (record kendi tipine eşlenemiyor).
- `StoreVariantDto` ve `CartItemDto` içindeki `VariantAttributeDto` listesi hesaplanan DTO'nun parçası olarak elle kaldı.
- `MappingConfigurationTests` hem `Compile()` hem `CompileProjection()` doğrular; özel alanlar `Common/MappingTests.cs` ile test edilir.

**Kabul:** Tüm testler yeşil; API yanıtları birebir aynı (entegrasyon testleri); EF projection'larında üretilen SQL'de fazladan kolon/`JOIN` yok; "elle kalır" listesi dışında `new XxxDto(` kalmaz.

**Tahmini süre:** Faz 1 ≈ 1–1,5 gün, Faz 2 ≈ 0,5 gün, Faz 3 ≈ 0,5 gün, Faz 4 ≈ 0,5 gün, Faz 5 ≈ 1,5–2 gün.

### Faz 6 — Solution ve disk yapısının eşitlenmesi
- [x] 6.1 `Trendyol.Client` solution'da `src` yerine `marketplaces` klasörüne alındı; solution klasörleri diskle aynı: `src/`, `marketplaces/`, `tests/`.
- [x] 6.2 Solution dışında kalan ve derlenmeyen `N11.Client`, `Hepsiburada.Client` ve `N11.Client.UnitTest` silindi. Gerekirse git geçmişinden geri alınabilir.
- [x] 6.3 Yerel diskteki eski build klasörleri (yalnızca `bin/obj`) temizlendi; `.dockerignore`'daki `clients/` satırı kaldırıldı.

---

## Kapsam dışı (ayrıca değerlendirilebilir)
- **Üç ayrı sonuç tipi:** `Result<T>`, `AuthResult<T>`, `MarketplaceResult<T>`. Birleştirmek davranış ve controller dönüşlerini değiştirir; bu plan yalnızca taşıma yaptığı için dışarıda.
- **Domain namespace'ini `ECommerce.Domain.*` yapmak:** EF model snapshot'ındaki tip adlarını değiştirir; ayrı ve dikkatli bir iş.
- ~~**`marketplaces/N11.Client` ve `Hepsiburada.Client`:** solution dışı, MVP dışı.~~ Faz 6'da silindi.

## Alınan kararlar
- **Alt klasör namespace'i:** `Dtos/` ve `Validators/` özellik namespace'ini kullanır (`ECommerce.Application.Catalog`); `using` değişikliği gerekmez. (2026-11-09)
- **Mapster:** Kaldırılmaz. Elle yazılmış eşlemeler Faz 5'te Mapster'a taşınır. (2026-11-09)
- **Controller klasörleri:** A — özelliğe göre; controller klasörü Application klasörüyle aynı adı taşır. (2026-11-09)

### Ek: Controller klasörleri, A ve B karşılaştırması
Yetki zaten koddan geliyor: fallback policy admin; müşteri controller'larında `[Authorize(Policy = Customer)]`, `StoreController`'da `[AllowAnonymous]`. Route'lar (`/store`, `/customer/...`, `/catalog/...`, `/orders`) iki seçenekte de **değişmez**.

**A — Özelliğe göre (Application ile ayna)**
```
Controllers/
  Auth/AuthController.cs                         /auth                 admin
  Catalog/CategoriesController.cs                /catalog/categories   admin
  Catalog/BrandsController.cs                    /catalog/brands       admin
  Catalog/ProductsController.cs                  /catalog/products     admin
  Catalog/StoreController.cs                     /store                anonim
  Customers/CustomerAuthController.cs            /customer/auth        anonim + müşteri
  Customers/CustomerAccountController.cs         /customer             müşteri
  Shopping/CartController.cs                     /customer/cart        müşteri
  Ordering/OrdersController.cs                   /orders               admin
  Ordering/CustomerOrdersController.cs           /customer/orders      müşteri
  Marketplaces/MarketplaceSyncController.cs      /marketplaces/sync    admin
  Marketplaces/Trendyol/*.cs (5 controller)      /marketplaces/trendyol/...
```
- Artı: Tek kural ("controller klasörü = Application klasörü"); `ICartService` nerede ise `CartController` da orada. Test klasörleriyle de aynı.
- Eksi: Admin ve müşteri uçları aynı klasörde karışık; bir uçun kime açık olduğu attribute'a bakmadan anlaşılmaz.

**B — Kanala göre (kim kullanıyor)**
```
Controllers/
  Admin/AuthController, Categories, Brands, Products, Orders, MarketplaceSync
  Admin/Trendyol/*.cs
  Store/StoreController, CustomerAuth, CustomerAccount, Cart, CustomerOrders
```
- Artı: Front-end'ler (vitrin, yönetim paneli) ayrı olacağı için her ekibin uçları tek klasörde; güvenlik incelemesi kolay.
- Eksi: Application özellik bazlı kaldığı için iki ayrı kural olur; `Ordering` servisleri iki klasörden kullanılır.

Seçilen: A. Kanal ayrımı zaten route (`/store`, `/customer`), OpenAPI tag'leri ve policy'lerle görünür.
