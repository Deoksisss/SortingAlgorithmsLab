using SortingLab.Core.Csv;
using SortingLab.Core.Models;

namespace SortingLab.Tests;

public sealed class CsvTests
{
    [Fact]
    public void Parse_CommaDelimited_WithQuotesAndNewlines()
    {
        const string csv = "Страна,Столица,Комментарий\r\n\"Коста-Рика\",\"Сан-Хосе\",\"строка, с запятой\"\r\n\"США\",\"Вашингтон\",\"первая\nвторая\"\r\n";
        var parsed = CsvTableReader.Parse(csv);

        Assert.Equal(',', parsed.Delimiter);
        Assert.Equal(new[] { "Страна", "Столица", "Комментарий" }, parsed.Table.Headers);
        Assert.Equal(2, parsed.Table.Records.Count);
        Assert.Equal("Коста-Рика", parsed.Table.Records[0].Field(0));
        Assert.Equal("строка, с запятой", parsed.Table.Records[0].Field(2));
        Assert.Equal("первая\nвторая", parsed.Table.Records[1].Field(2));
    }

    [Theory]
    [InlineData("a;b;c\n1;2;3", ';')]
    [InlineData("a\tb\tc\n1\t2\t3", '\t')]
    [InlineData("a|b|c\n1|2|3", '|')]
    public void Parse_DetectsDelimiter(string csv, char expected)
    {
        var parsed = CsvTableReader.Parse(csv);
        Assert.Equal(expected, parsed.Delimiter);
        Assert.Equal(3, parsed.Table.Headers.Count);
        Assert.Single(parsed.Table.Records);
    }

    [Fact]
    public void Parse_EmptyValues_AndUnevenRows()
    {
        const string csv = "a,b,c\n1,,3\n4,5"; // во второй строке не хватает столбца
        var parsed = CsvTableReader.Parse(csv);

        Assert.Equal(2, parsed.Table.Records.Count);
        Assert.Equal(string.Empty, parsed.Table.Records[0].Field(1));
        Assert.Equal(string.Empty, parsed.Table.Records[1].Field(2));
        Assert.Contains(parsed.Warnings, warning => warning.Contains("не хватает"));
    }

    [Fact]
    public void Parse_EmptyAndDuplicateHeaders_ProducesUniqueHeaders()
    {
        var parsed = CsvTableReader.Parse("a,,a\n1,2,3");
        Assert.Equal("a", parsed.Table.Headers[0]);
        Assert.Equal("Столбец 2", parsed.Table.Headers[1]);
        Assert.Equal("a (2)", parsed.Table.Headers[2]);
        Assert.NotEmpty(parsed.Warnings);
    }

    [Fact]
    public void Parse_HeaderOnly_ReturnsEmptyTable()
    {
        var parsed = CsvTableReader.Parse("a,b,c");
        Assert.Equal(3, parsed.Table.Headers.Count);
        Assert.Empty(parsed.Table.Records);
    }

    [Fact]
    public void Parse_EmptyFile_Throws()
    {
        Assert.Throws<CsvFormatException>(() => CsvTableReader.Parse("   "));
    }

    [Fact]
    public void Writer_RoundTrips_AllFields()
    {
        var table = new CsvTable(
            new[] { "a", "b", "c" },
            new[]
            {
                new CsvRecord(0, new[] { "1", "с, запятой", "кавычка \"внутри\"" }),
                new CsvRecord(1, new[] { "2", "перенос\nстроки", string.Empty })
            });

        var text = CsvTableWriter.WriteToString(table);
        var parsed = CsvTableReader.Parse(text);

        Assert.Equal(table.Headers, parsed.Table.Headers);
        Assert.Equal(2, parsed.Table.Records.Count);
        Assert.Equal("с, запятой", parsed.Table.Records[0].Field(1));
        Assert.Equal("кавычка \"внутри\"", parsed.Table.Records[0].Field(2));
        Assert.Equal("перенос\nстроки", parsed.Table.Records[1].Field(1));
    }

    [Fact]
    public void SortKeyComparer_TextIsCaseInsensitive_AndStableForEqualKeys()
    {
        var comparer = new SortKeyComparer(0, SortKeyType.Text, SortDirection.Ascending);
        var a = new CsvRecord(1, new[] { "apple" });
        var b = new CsvRecord(2, new[] { "Apple" });
        var c = new CsvRecord(3, new[] { "banana" });

        Assert.Equal(0, comparer.Compare(a, b));
        Assert.True(comparer.Compare(a, c) < 0);
    }

    [Fact]
    public void SortKeyComparer_NumericAndDecimal_ParseInvariantly()
    {
        var integerComparer = new SortKeyComparer(0, SortKeyType.Integer, SortDirection.Ascending);
        Assert.True(integerComparer.Compare(
            new CsvRecord(0, new[] { "-10" }),
            new CsvRecord(1, new[] { "3" })) < 0);

        var decimalComparer = new SortKeyComparer(0, SortKeyType.Decimal, SortDirection.Ascending);
        Assert.True(decimalComparer.Compare(
            new CsvRecord(0, new[] { "1,5" }),
            new CsvRecord(1, new[] { "2.25" })) < 0);
    }

    [Fact]
    public void SortKeyComparer_EmptyValues_AreFirstAscending_AndLastDescending()
    {
        var ascending = new SortKeyComparer(0, SortKeyType.Integer, SortDirection.Ascending);
        var empty = new CsvRecord(0, new[] { string.Empty });
        var value = new CsvRecord(1, new[] { "5" });
        Assert.True(ascending.Compare(empty, value) < 0);
        Assert.True(ascending.Compare(value, empty) > 0);

        var descending = new SortKeyComparer(0, SortKeyType.Integer, SortDirection.Descending);
        Assert.True(descending.Compare(empty, value) > 0);
    }

    [Fact]
    public void SortKeyComparer_InvalidNumeric_IsReported()
    {
        var comparer = new SortKeyComparer(0, SortKeyType.Integer, SortDirection.Ascending);
        var errors = comparer.Validate(new[]
        {
            new CsvRecord(0, new[] { "12" }),
            new CsvRecord(1, new[] { "не число" }),
            new CsvRecord(2, new[] { string.Empty })
        });

        Assert.Single(errors);
        Assert.Equal(1, errors[0].RecordId);
    }
}
