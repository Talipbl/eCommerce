using ECommerce.Core.Domain;
using ECommerce.Customers;

namespace ECommerce.UnitTests.Customers;

public class CustomerTests
{
    private static Customer NewCustomer() => Customer.Register("  Ali@Example.COM ", "Ali", "Veli", null);

    private static CustomerAddress Address(Customer c, string title) =>
        c.AddAddress(title, "Ali Veli", "05551112233", "İstanbul", "Kadıköy", "Moda Cd. No:1", null);

    [Fact]
    public void Email_is_normalized_and_validated()
    {
        NewCustomer().Email.ShouldBe("ali@example.com");
        Should.Throw<DomainException>(() => Customer.Register("gecersiz", "Ali", "Veli", null));
    }

    [Fact]
    public void First_address_becomes_default_and_default_can_change()
    {
        var customer = NewCustomer();
        var home = Address(customer, "Ev");
        var work = Address(customer, "İş");

        home.IsDefault.ShouldBeTrue();
        work.IsDefault.ShouldBeFalse();

        customer.SetDefaultAddress(work);

        home.IsDefault.ShouldBeFalse();
        work.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Removing_default_address_promotes_another()
    {
        var customer = NewCustomer();
        var home = Address(customer, "Ev");
        var work = Address(customer, "İş");

        customer.RemoveAddress(home);

        customer.Addresses.ShouldHaveSingleItem().ShouldBe(work);
        work.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Address_count_is_limited()
    {
        var customer = NewCustomer();
        for (var i = 0; i < Customer.MaxAddresses; i++)
            Address(customer, $"Adres {i}");

        Should.Throw<DomainException>(() => Address(customer, "Fazla"));
    }

    [Fact]
    public void Repeated_failures_lock_the_account()
    {
        var customer = NewCustomer();
        var now = DateTime.UtcNow;

        for (var i = 0; i < 3; i++)
            customer.RegisterFailedLogin(now, maxAttempts: 3, TimeSpan.FromMinutes(15));

        customer.IsLockedOut(now).ShouldBeTrue();
        customer.Role.ShouldBe(CustomerRoles.Customer);
        customer.DisplayName.ShouldBe("ali@example.com");
    }
}
