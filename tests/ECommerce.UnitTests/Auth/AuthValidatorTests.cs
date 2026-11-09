using ECommerce.Application.Auth;

namespace ECommerce.UnitTests.Auth;

public class AuthValidatorTests
{
    [Theory]
    [InlineData("", "parola")]
    [InlineData("admin", "")]
    public void Login_requires_user_name_and_password(string userName, string password)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest(userName, password));

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Kisa-1")]
    [InlineData("sadecekucukharf123")]
    [InlineData("Eski-Parola-123")]
    public void New_password_must_be_strong_and_different(string newPassword)
    {
        var result = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest("Eski-Parola-123", newPassword));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Strong_new_password_is_accepted()
    {
        var result = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest("Eski-Parola-123", "Yeni-Parola-456"));

        result.IsValid.ShouldBeTrue();
    }
}
