using FastVitae.Model;
using Xunit;

namespace FastVitae.Tests;

public class CurrencyTests
{
    [Fact]
    public void PinnedFirst_InOrder() =>
        Assert.Equal(new[] { "USD", "EUR", "GBP", "CAD", "AUD" }, Currency.All.Take(5).Select(c => c.Code));

    [Fact]
    public void Euro_HasEuFlag() => Assert.Equal("EU", Currency.Find("EUR")!.Iso);

    [Fact]
    public void EuroCountries_NotListedAlone()
    {
        Assert.DoesNotContain(Currency.All, c => c.Iso == "DE");
        Assert.Single(Currency.All, c => c.Code == "EUR");
    }

    [Fact]
    public void Rest_AlphabeticalByCountry()
    {
        var names = Currency.All.Skip(5).Select(c => c.CountryName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public void PinnedCountries_NotRepeated() =>
        Assert.DoesNotContain(Currency.All.Skip(5), c => c.Iso is "US" or "GB" or "CA" or "AU");

    [Fact]
    public void Default_IsUsd() => Assert.Equal("USD", Currency.Default.Code);

    [Fact]
    public void Poland_FindsZloty()
    {
        var zloty = Currency.All.Single(c => c.Iso == "PL");
        Assert.Equal("PLN", zloty.Code);
        Assert.True(Currency.Matches(zloty, "poland"));
        Assert.True(Currency.Matches(zloty, "Zloty"));
        Assert.True(Currency.Matches(zloty, "PLN"));
        Assert.False(Currency.Matches(zloty, "Brazil"));
    }

    [Fact]
    public void Germany_FindsEuro() => Assert.True(Currency.Matches(Currency.Find("EUR")!, "germ"));
}

public class CountryOrderTests
{
    [Fact]
    public void PinnedFirst_InOrder() =>
        Assert.Equal(new[] { "US", "GB", "CA", "AU" }, Country.Ordered.Take(4).Select(c => c.Iso));

    [Fact]
    public void Rest_AlphabeticalWithoutPinned()
    {
        var rest = Country.Ordered.Skip(4).ToList();
        Assert.DoesNotContain(rest, c => c.Iso is "US" or "GB" or "CA" or "AU");
        Assert.Equal(Country.All.Count, Country.Ordered.Count);
    }

    [Fact]
    public void Matches_NameOrDialCode()
    {
        var paraguay = Country.Find("PY")!;
        Assert.True(Country.Matches(paraguay, "para"));
        Assert.True(Country.Matches(paraguay, "+595"));
        Assert.False(Country.Matches(paraguay, "Peru"));
    }
}
