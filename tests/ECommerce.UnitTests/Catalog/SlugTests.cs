using ECommerce.Core.Text;

namespace ECommerce.UnitTests.Catalog;

public class SlugTests
{
    [Theory]
    [InlineData("Kadın Tişört & Atlet", "kadin-tisort-atlet")]
    [InlineData("  ÇOCUK  Giyim  ", "cocuk-giyim")]
    [InlineData("Ağır Şömine Üstü Işık", "agir-somine-ustu-isik")]
    [InlineData("İstanbul -- Kadıköy!", "istanbul-kadikoy")]
    [InlineData("Model 2026/X", "model-2026-x")]
    public void Turkish_text_is_converted_to_url_slug(string text, string expected) =>
        Slug.From(text).ShouldBe(expected);

    [Fact]
    public void Slug_is_limited_to_100_characters() =>
        Slug.From(new string('a', 150)).Length.ShouldBe(100);

    [Theory]
    [InlineData("kadin-tisort", true)]
    [InlineData("Kadin-Tisort", false)]
    [InlineData("kadin tisort", false)]
    [InlineData("-kadin", false)]
    public void Slug_validation(string slug, bool valid) =>
        Slug.IsValid(slug).ShouldBe(valid);
}
