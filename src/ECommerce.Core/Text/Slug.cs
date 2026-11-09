using System.Text;
using System.Text.RegularExpressions;

namespace ECommerce.Core.Text
{
    public static partial class Slug
    {
        public const int MaxLength = 100;

        public static string From(string text)
        {
            var builder = new StringBuilder(text.Length);
            foreach (var c in text.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")))
            {
                builder.Append(c switch
                {
                    'ç' => 'c',
                    'ğ' => 'g',
                    'ı' => 'i',
                    'ö' => 'o',
                    'ş' => 's',
                    'ü' => 'u',
                    _ when char.IsAsciiLetterOrDigit(c) => c,
                    _ => '-'
                });
            }

            var slug = MultipleDashes().Replace(builder.ToString(), "-").Trim('-');
            return slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');
        }

        public static bool IsValid(string? slug) =>
            !string.IsNullOrEmpty(slug) && slug.Length <= MaxLength && ValidSlug().IsMatch(slug);

        [GeneratedRegex("-{2,}")]
        private static partial Regex MultipleDashes();

        [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
        private static partial Regex ValidSlug();
    }
}
