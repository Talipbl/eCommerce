using FluentValidation;
using Trendyol.Client.Orders;

namespace ECommerce.Application.Marketplaces.Trendyol;

public class PackageLinesRequestValidator : AbstractValidator<PackageLinesRequest>
{
    public PackageLinesRequestValidator() => RuleFor(x => x.Lines).PackageLines();
}

public class InvoicePackageRequestValidator : AbstractValidator<InvoicePackageRequest>
{
    public InvoicePackageRequestValidator()
    {
        RuleFor(x => x.Lines).PackageLines();
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
    }
}

public class UnsuppliedPackageRequestValidator : AbstractValidator<UnsuppliedPackageRequest>
{
    public UnsuppliedPackageRequestValidator()
    {
        RuleFor(x => x.Lines).PackageLines();
        RuleFor(x => x.Reason).IsInEnum();
    }
}

public class SendInvoiceLinkCommandValidator : AbstractValidator<SendInvoiceLinkCommand>
{
    public SendInvoiceLinkCommandValidator()
    {
        RuleFor(x => x.InvoiceLink).NotEmpty()
            .Must(l => Uri.TryCreate(l, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("Fatura linki https olmalıdır.");
        RuleFor(x => x.InvoiceNumber!)
            .Matches("^[A-Za-z0-9]{3}20[2-9][0-9][0-9]{9}$")
            .When(x => x.InvoiceNumber is not null)
            .WithMessage("Fatura numarası 3 karakter + yıl (2020-2099) + 9 haneli sıra numarası biçiminde olmalıdır.");
    }
}

internal static class PackageLineRules
{
    public static IRuleBuilderOptions<T, IReadOnlyList<PackageLine>> PackageLines<T>(this IRuleBuilder<T, IReadOnlyList<PackageLine>> rule) =>
        rule.NotEmpty()
            .Must(lines => lines.All(l => l.LineId > 0 && l.Quantity > 0)).WithMessage("Satır numarası ve adet pozitif olmalıdır.")
            .Must(lines => lines.Select(l => l.LineId).Distinct().Count() == lines.Count).WithMessage("Aynı satır birden fazla gönderilemez.");
}
