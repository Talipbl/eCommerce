namespace ECommerce.Application.Customers;

public record SaveAddressRequest(string Title, string FullName, string Phone, string City, string District, string AddressLine, string? PostalCode);

public record AddressDto(int Id, string Title, string FullName, string Phone, string City, string District, string AddressLine, string? PostalCode, bool IsDefault);
