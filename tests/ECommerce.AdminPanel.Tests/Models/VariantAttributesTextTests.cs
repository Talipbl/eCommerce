using ECommerce.AdminPanel.Models;

namespace ECommerce.AdminPanel.Tests.Models;

public class VariantAttributesTextTests
{
    [Fact]
    public void Lines_are_parsed_and_formatted_back()
    {
        VariantAttributesText.TryParse(" Beden : M \r\n\r\nRenk: Lacivert: koyu", out var attributes).ShouldBeTrue();

        attributes.Select(a => (a.Name, a.Value)).ShouldBe([("Beden", "M"), ("Renk", "Lacivert: koyu")]);
        VariantAttributesText.Format(attributes).ShouldBe("Beden: M\nRenk: Lacivert: koyu");
    }

    [Theory]
    [InlineData("Beden M")]
    [InlineData(": M")]
    [InlineData("Beden:")]
    public void Line_without_name_or_value_is_rejected(string text)
    {
        VariantAttributesText.TryParse(text, out _).ShouldBeFalse();
    }
}
