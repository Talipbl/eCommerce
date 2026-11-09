# Kod düzeni kuralları

Yeni kod bu düzene uyar. Gerekçe ve geçmiş için bkz. [YAPI-DUZENLEME-PLANI.md](YAPI-DUZENLEME-PLANI.md).

## Solution

Solution klasörleri disk klasörleriyle aynıdır: `src/` (uygulama katmanları), `marketplaces/` (pazaryeri HTTP client'ları), `tests/`. Diskte olup solution'da olmayan proje tutulmaz.

## Application (`src/ECommerce.Application`)

Özellik (feature) klasörleri: `Auth`, `Catalog`, `Customers`, `Shopping`, `Ordering`, `Marketplaces`, `Marketplaces/Trendyol`, `Common`.

```
<Özellik>/
  Dtos/<Alan>Dtos.cs              servis başına request + response record'ları
  Validators/<Alan>Validators.cs  DTO dosyasıyla eşleşen validator'lar
  Mappings/<Özellik>MappingConfig.cs  Mapster IRegister eşlemeleri
  IXxxService.cs                  yalnızca interface
  XxxService.cs                   yalnızca implementasyon
  XxxOptions.cs, yardımcılar      her biri kendi dosyasında
Common/
  Abstractions/                   dış dünya portları (IFileStorage, IMediaUrlBuilder)
  Mapping/                        Mapster kaydı
  Results.cs                      Result<T>, PagedResult<T>, AppError
```

- **Namespace özellik seviyesindedir:** `Catalog/Dtos/BrandDtos.cs` → `ECommerce.Application.Catalog`. `Dtos/` ve `Validators/` yeni namespace açmaz.
- Bir dosyada tek rol: interface, servis, DTO grubu, validator grubu, options ya da yardımcı. Karışık dosya açılmaz.
- DTO adları uygulama genelinde benzersizdir; dış sisteme ait olanlar önek alır (`TrendyolBrandDto`).
- Entity/dış model → DTO kopyalaması Mapster ile yapılır: servis `IMapper` enjekte eder, EF sorgularında `ProjectToType<T>(mapper.Config)` kullanılır. Hesaplama, iş kuralı ya da ağaç kurma içeren dönüşümler elle yazılır.
- Config `RequireExplicitMapping` ile çalışır; her yeni eşleme ilgili `MappingConfig`'e eklenmeli, yoksa uygulama açılışta hata verir.
- DI kaydı `DependencyInjection.cs` içinde, özelliğin private metodunda (`AddCatalog()`, `AddOrdering()` ...) yapılır.

## API (`src/ECommerce.API`)

```
Controllers/<Özellik>/XxxController.cs   dosya başına bir controller; klasör adı Application özelliğiyle aynı
Infrastructure/Http|Security|Media|Seed  namespace: ECommerce.API.Infrastructure
BackgroundJobs/                          worker'lar ve AddBackgroundJobs()
```

- Controller namespace'i klasörü izler (`ECommerce.API.Controllers.Shopping`).
- Kanal (admin / vitrin / müşteri) klasörle değil route, `[Tags]` ve policy ile ayrılır. Varsayılan policy admin'dir; müşteri uçları `CustomerPolicy`, vitrin `[AllowAnonymous]` kullanır.
- Yalnızca HTTP'ye özgü tipler (query filtreleri, `AccessTokenResponse`) API'de, kendi dosyasında durur; diğer request/response tipleri Application'dadır.

## Domain (`src/ECommerce.Domain`)

- Klasör: `ECommerce.Domain/<Özellik>/`; namespace `ECommerce.<Özellik>` (EF model snapshot'ı bu adlara bağlı, değiştirilmez).
- Aggregate root başına bir dosya. O köke ait child entity, value object ve enum'lar aynı dosyada kalabilir (`Product` + `ProductVariant` + `ProductImage`).

## Testler

- `tests/ECommerce.UnitTests` klasörleri Application özelliklerini aynalar (`Marketplaces/Trendyol/TrendyolOrderServiceTests.cs`).
- Entegrasyon testleri uç gruplarına göre düz durur (`CatalogEndpointsTests.cs`).
