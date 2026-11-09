namespace ECommerce.API.Infrastructure;

public class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Sistem";
    public string LastName { get; set; } = "Yöneticisi";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);
}
