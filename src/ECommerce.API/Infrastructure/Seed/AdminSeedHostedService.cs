using ECommerce.Admin;
using ECommerce.Application.Auth;
using ECommerce.Core.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.API.Infrastructure;

/// <summary>
/// "Seed:Admin" ayarları verilmişse ve kullanıcı yoksa ilk yöneticiyi oluşturur.
/// </summary>
public class AdminSeedHostedService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<AdminSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = configuration.GetSection(AdminSeedOptions.SectionName).Get<AdminSeedOptions>();
        if (options is null || !options.IsConfigured)
            return;

        var passwordCheck = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest("-", options.Password));
        if (!passwordCheck.IsValid)
            throw new InvalidOperationException($"Seed:Admin:Password parola kurallarını sağlamıyor: {passwordCheck}");

        using var scope = scopeFactory.CreateScope();
        var admins = scope.ServiceProvider.GetRequiredService<IRepository<DefAdmin>>();
        if (await admins.AnyAsync(a => a.UserName == options.UserName, cancellationToken))
            return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<DefAdmin>>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var admin = new DefAdmin
        {
            UserName = options.UserName,
            FirstName = options.FirstName,
            LastName = options.LastName,
            MailAdress = options.Email,
            RegisterDate = clock.GetUtcNow().UtcDateTime,
            Role = AdminRoles.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, options.Password);

        admins.Add(admin);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        logger.LogInformation("İlk yönetici oluşturuldu: {UserName}", options.UserName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
