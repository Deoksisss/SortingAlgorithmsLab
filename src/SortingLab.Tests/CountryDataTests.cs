using System.Globalization;
using SortingLab.Core.Csv;

namespace SortingLab.Tests;

public sealed class CountryDataTests
{
    [Fact]
    public void BuiltInDataset_ContainsCompleteValidCountryTable()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "countries.csv");
        var parsed = CsvTableReader.ReadFile(path);
        var table = parsed.Table;

        Assert.Equal(5, table.Headers.Count);
        Assert.Equal(195, table.Records.Count);
        Assert.Equal(195, table.Records.Select(record => record.Field(0)).Distinct().Count());

        var continents = table.Records.Select(record => record.Field(1)).Distinct().ToHashSet();
        Assert.Equal(6, continents.Count);

        foreach (var record in table.Records)
        {
            Assert.False(string.IsNullOrWhiteSpace(record.Field(0)));
            Assert.False(string.IsNullOrWhiteSpace(record.Field(1)));
            Assert.False(string.IsNullOrWhiteSpace(record.Field(2)));
            Assert.True(long.TryParse(record.Field(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out var population));
            Assert.True(population > 0);
            Assert.True(double.TryParse(record.Field(3), NumberStyles.Float, CultureInfo.InvariantCulture, out var area));
            Assert.True(area > 0);
        }
    }
}
