using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Serilog;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApiClients(builder.Configuration);
builder.Services.AddAdminAuthentication(builder.Configuration);
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<SessionExpiredFilter>();
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "admin_csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
// Türkçe karakterler HTML'de &#x131; gibi kodlanmasın; Razor yine HTML'e özel karakterleri kodlar.
builder.Services.AddWebEncoders(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
// Form değerleri (fiyat vb.) sunucunun kültüründen bağımsız, nokta ondalıkla okunur; Türkçe biçim yalnızca gösterimde (Display).
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture],
    RequestCultureProviders = []
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapHealthChecks("/health/live").AllowAnonymous();

app.Run();

public partial class Program;
