using ECommerce.Accounts;
using ECommerce.Core.Domain;
using System.Net.Mail;

namespace ECommerce.Customers;

public class Customer : AggregateRoot<int>, IAccount
{
    public const int MaxAddresses = 10;

    private readonly List<CustomerAddress> _addresses = [];

    public string Email { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEndDate { get; private set; }

    public DateTime? LastLoginDate { get; private set; }

    public IReadOnlyList<CustomerAddress> Addresses => _addresses;

    public string DisplayName => Email;

    public string Role => CustomerRoles.Customer;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static Customer Register(string email, string firstName, string lastName, string? phone)
    {
        var normalized = NormalizeEmail(email);
        if (!MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized || normalized.Length > 150)
            throw new DomainException("Geçerli bir e-posta adresi girilmelidir.");

        var customer = new Customer { Email = normalized };
        customer.UpdateProfile(firstName, lastName, phone);
        return customer;
    }

    public void UpdateProfile(string firstName, string lastName, string? phone)
    {
        FirstName = Required(firstName, "Ad", 50);
        LastName = Required(lastName, "Soyad", 50);
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public bool IsLockedOut(DateTime now) => LockoutEndDate > now;

    public void RegisterFailedLogin(DateTime now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount < maxAttempts)
            return;

        LockoutEndDate = now.Add(lockoutDuration);
        FailedLoginCount = 0;
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        FailedLoginCount = 0;
        LockoutEndDate = null;
        LastLoginDate = now;
    }

    public CustomerAddress AddAddress(string title, string fullName, string phone, string city, string district, string addressLine, string? postalCode)
    {
        if (_addresses.Count >= MaxAddresses)
            throw new DomainException($"En fazla {MaxAddresses} adres kaydedilebilir.");

        var address = new CustomerAddress();
        address.Update(title, fullName, phone, city, district, addressLine, postalCode);
        address.IsDefault = _addresses.Count == 0;
        _addresses.Add(address);
        return address;
    }

    public void SetDefaultAddress(CustomerAddress address)
    {
        foreach (var a in _addresses)
            a.IsDefault = ReferenceEquals(a, address);
    }

    public void RemoveAddress(CustomerAddress address)
    {
        _addresses.Remove(address);
        if (address.IsDefault && _addresses.Count > 0)
            _addresses[0].IsDefault = true;
    }

    internal static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} boş olamaz.");
        if (value.Trim().Length > maxLength)
            throw new DomainException($"{field} en fazla {maxLength} karakter olabilir.");
        return value.Trim();
    }
}

public class CustomerAddress : AggregateRoot<int>
{
    public int CustomerId { get; private set; }

    public string Title { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public string Phone { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string District { get; private set; } = null!;

    public string AddressLine { get; private set; } = null!;

    public string? PostalCode { get; private set; }

    public bool IsDefault { get; internal set; }

    public void Update(string title, string fullName, string phone, string city, string district, string addressLine, string? postalCode)
    {
        Title = Customer.Required(title, "Adres başlığı", 50);
        FullName = Customer.Required(fullName, "Alıcı adı", 100);
        Phone = Customer.Required(phone, "Telefon", 20);
        City = Customer.Required(city, "İl", 50);
        District = Customer.Required(district, "İlçe", 50);
        AddressLine = Customer.Required(addressLine, "Adres", 300);
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
    }
}
