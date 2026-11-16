using System.Globalization;

namespace FastVitae.Model;

// One row in the currency dropdown
public class Currency
{
    public string Code { get; }          // for example "PLN"
    public string Symbol { get; }        // for example "zł"
    public string Name { get; }          // for example "Polish Zloty"
    public string Iso { get; }           // flag, "EU" for Euro
    public string CountryName { get; }
    private readonly List<string> searchNames;

    public string Flag => $"pack://application:,,,/FamFamFam.Flags.Wpf;component/images/{Iso.ToLowerInvariant()}.png";

    private Currency(string code, string symbol, string name, string iso, string countryName, List<string> searchNames)
    {
        Code = code;
        Symbol = symbol;
        Name = name;
        Iso = iso;
        CountryName = countryName;
        this.searchNames = searchNames;
    }

    // Pinned first, then by country
    public static readonly List<Currency> All = Build();

    public static Currency Default => All[0];

    public static Currency? Find(string code) => All.FirstOrDefault(currency => currency.Code == code);

    // Country, currency name or code
    public static bool Matches(Currency currency, string text)
    {
        text = text.Trim();
        return text == ""
            || currency.Code.Contains(text, StringComparison.OrdinalIgnoreCase)
            || currency.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || currency.searchNames.Any(name => name.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static List<Currency> Build()
    {
        // Each country's own currency
        var byCountry = new List<(Country Country, RegionInfo Region)>();
        foreach (var country in Country.All)
        {
            try
            {
                byCountry.Add((country, new RegionInfo(country.Iso)));
            }
            catch (ArgumentException)
            {
                // No currency data: skip
            }
        }

        var euroCountries = byCountry.Where(c => c.Region.ISOCurrencySymbol == "EUR").Select(c => c.Country.Name).ToList();
        var euro = new Currency("EUR", "€", "Euro", "EU", "European Union", euroCountries.Prepend("European Union").ToList());

        Currency FromCountry(Country country, RegionInfo region) =>
            new(region.ISOCurrencySymbol, region.CurrencySymbol, region.CurrencyEnglishName, country.Iso, country.Name, new List<string> { country.Name });

        string[] pinnedIsos = { "US", "GB", "CA", "AU" };
        var pinned = pinnedIsos.Select(iso => byCountry.First(c => c.Country.Iso == iso)).Select(c => FromCountry(c.Country, c.Region)).ToList();
        pinned.Insert(1, euro);

        var rest = byCountry
            .Where(c => !pinnedIsos.Contains(c.Country.Iso) && c.Region.ISOCurrencySymbol != "EUR")
            .OrderBy(c => c.Country.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => FromCountry(c.Country, c.Region));
        return pinned.Concat(rest).ToList();
    }
}
