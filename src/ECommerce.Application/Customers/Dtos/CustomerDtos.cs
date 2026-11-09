namespace ECommerce.Application.Customers;

public record RegisterCustomerRequest(string Email, string Password, string FirstName, string LastName, string? Phone);

public record UpdateCustomerProfileRequest(string FirstName, string LastName, string? Phone);

public record CustomerProfileDto(int Id, string Email, string FirstName, string LastName, string? Phone);
