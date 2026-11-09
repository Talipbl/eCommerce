using Asp.Versioning;
using ECommerce.API.BackgroundJobs;
using ECommerce.API.Infrastructure;
using ECommerce.Application;
using ECommerce.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Derleme zamanı OpenAPI üretimi uygulamayı kısa süre başlatır; zorunlu ayarlar yer tutucuyla doldurulur, arka plan işleri çalışmaz.
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (isOpenApiGeneration)
    builder.Configuration.AddInMemoryCollection([
        new("ConnectionStrings:ECommerce", "Server=openapi-build"),
        new("Jwt:SigningKey", new string('x', 32)),
        new("Trendyol:SellerId", "0"),
        new("Trendyol:ApiKey", "openapi-build"),
        new("Trendyol:ApiSecret", "openapi-build")
    ]);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddMedia(builder.Configuration);

if (!isOpenApiGeneration)
    builder.Services.AddBackgroundJobs();

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// OpenAPI şeması MVC'nin değil HTTP JSON ayarlarından üretilir; enum'lar string, sayılar yalnızca sayı olarak yazılsın.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi(options =>
    {
        // NSwag 3.1'deki tip dizilerini tam desteklemediği için panel client'ı 3.0 dokümandan üretilir.
        options.Document.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
        options.Document.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "ECommerce API";
            document.Info.Description = null;
            return Task.CompletedTask;
        });
        options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.Document.AddSchemaTransformer((schema, context, _) =>
        {
            // Enum şemasına tip yazılmadığında client üreticiler değeri sayı sanıyor; API enum'ları string döner.
            var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
            if (type.IsEnum)
                schema.Type = JsonSchemaType.String;
            return Task.CompletedTask;
        });
        options.Document.AddOperationTransformer((operation, context, _) =>
        {
            // Üretilen client'ta controller başına bir sınıf ve okunur metot adları için: Products_List → ProductsClient.ListAsync.
            if (context.Description.ActionDescriptor is ControllerActionDescriptor action)
                operation.OperationId = $"{action.ControllerName}_{action.ActionName}";
            return Task.CompletedTask;
        });
    });

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ECommerceContext>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMedia();
app.UseCors(SecurityRegistration.FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.Run();

public partial class Program;
