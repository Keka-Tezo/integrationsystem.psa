using oculusit.sync.connectwise.modules;
using oculusit.sync.keka.modules;

namespace oculusit.sync.orchestration.mappings;

public static class KekaClientMapper
{
    private const string FallbackEmail = "blank_customer_email@oculusit.com";

    // Well-known country name → ISO 3166-1 alpha-2 code mappings
    private static readonly Dictionary<string, string> _countryCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["united states"]  = "US",
        ["usa"]            = "US",
        ["canada"]         = "CA",
        ["united kingdom"] = "GB",
        ["uk"]             = "GB",
        ["australia"]      = "AU",
        ["india"]          = "IN",
        ["germany"]        = "DE",
        ["france"]         = "FR",
        ["singapore"]      = "SG",
    };

    public static KekaClientRequest MapToKekaClientRequest(
        ConnectWiseCompany company,
        string? usdCurrencyId = null)
    {
        var addressLine1 = NullIfEmpty(company.AddressLine1);
        var addressLine2 = NullIfEmpty(company.AddressLine2);
        var city         = NullIfEmpty(company.City);
        var state        = NullIfEmpty(company.State);
        var zip          = NullIfEmpty(company.Zip);
        var countryCode  = ResolveCountryCode(company.Country?.Name);

        var hasBillingAddress = addressLine1 is not null || city is not null
                             || state is not null || zip is not null;

        KekaBillingAddress? billingAddress = hasBillingAddress
            ? new KekaBillingAddress
            {
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                CountryCode  = countryCode,
                City         = city,
                State        = state,
                Zip          = zip
            }
            : null;

        KekaBillingInfo? billingInfo = (billingAddress is not null || usdCurrencyId is not null)
            ? new KekaBillingInfo
            {
                BillingCurrencyId = usdCurrencyId,
                BillingAddress    = billingAddress
            }
            : null;

        return new KekaClientRequest
        {
            Name        = company.Name,
            Description = NullIfEmpty(company.Identifier),
            Code        = company.Id,
            Phone       = NullIfEmpty(company.PhoneNumber),
            Website     = NullIfEmpty(company.Website),
            Email       = string.IsNullOrWhiteSpace(company.InvoiceCCEmailAddress)
                            ? FallbackEmail
                            : company.InvoiceCCEmailAddress,
            BillingInfo = billingInfo
        };
    }

    private static string? ResolveCountryCode(string? countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
            return null;

        if (countryName.Length == 2)
            return countryName.ToUpperInvariant();

        return _countryCodeMap.TryGetValue(countryName, out var code)
            ? code
            : null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
