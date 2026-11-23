using SortingLab.Core.Algorithms.Text;
using SortingLab.Core.Text;

namespace SortingLab.Tests;

public sealed class TextSortTests
{
    private static readonly string[] PunctuationText =
    {
        "Привет, мир!", "Hello... world", "АЛГОРИТМ", "алгоритм", "data", "DATA", "  ", "ёж", "ель"
    };

    [Theory]
    [InlineData("")]
    [InlineData("Привет, мир! Hello... world 123")]
    [InlineData("  Много   пробелов\nи переносов  ")]
    [InlineData("!!! ... ---")]
    public void Tokenizer_HandlesText(string text)
    {
        var words = TextTokenizer.Tokenize(text);
        Assert.All(words, word => Assert.DoesNotContain(' ', word));
        Assert.All(words, word => Assert.Equal(word.ToLowerInvariant(), word));
    }

    [Fact]
    public void Tokenizer_NormalizesCaseAndPunctuation()
    {
        var words = TextTokenizer.Tokenize("Привет, МИР! Hello... world 123; тест-слово");
        Assert.Equal(new[] { "привет", "мир", "hello", "world", "123", "тест", "слово" }, words);
    }

    [Fact]
    public void Tokenizer_EmptyText_ReturnsEmpty()
    {
        Assert.Empty(TextTokenizer.Tokenize(string.Empty));
        Assert.Empty(TextTokenizer.Tokenize("   ... ,,, "));
    }

    public static IEnumerable<object[]> SortCases()
    {
        yield return new object[] { Array.Empty<string>() };
        yield return new object[] { new[] { "apple" } };
        yield return new object[] { new[] { "b", "a", "c", "a" } };
        yield return new object[] { new[] { "10", "2", "1", "10" } };
        yield return new object[] { new[] { "я", "а", "ё", "е", "Hello", "hello" } };
        yield return new object[] { new[] { "", "a", "ab", "abc" } };
    }

    [Theory]
    [MemberData(nameof(SortCases))]
    public void QuickSortAndRadixSort_Agree(string[] words)
    {
        var expected = words.OrderBy(word => word, StringComparer.Ordinal).ToArray();
        var quick = new QuickSort().Sort(words);
        var radix = new RadixSort().Sort(words);

        Assert.Equal(expected, quick);
        Assert.Equal(expected, radix);
        Assert.Equal(quick, radix);
    }

    [Fact]
    public void BothAlgorithms_HandleLargeRandomSet()
    {
        var vocabulary = TextGenerator.GetVocabulary(TextLanguage.Mixed);
        var random = new Random(777);
        var words = Enumerable.Range(0, 5000)
            .Select(_ => vocabulary[random.Next(vocabulary.Count)])
            .ToArray();

        var quick = new QuickSort().Sort(words);
        var radix = new RadixSort().Sort(words);

        Assert.Equal(quick, radix);
        for (var i = 1; i < quick.Length; i++)
        {
            Assert.True(StringComparer.Ordinal.Compare(quick[i - 1], quick[i]) <= 0);
        }
    }

    [Fact]
    public void BothAlgorithms_HandleAlreadySortedAndEqualArrays()
    {
        var sorted = Enumerable.Range(0, 300).Select(i => $"word{i:D4}").ToArray();
        var equal = Enumerable.Repeat("одинаково", 300).ToArray();

        foreach (var input in new[] { sorted, equal })
        {
            var quick = new QuickSort().Sort(input);
            var radix = new RadixSort().Sort(input);
            Assert.Equal(input, quick);
            Assert.Equal(input, radix);
        }
    }

    [Fact]
    public void QuickSort_HandlesAdversarialPatterns()
    {
        var random = new Random(99);
        var patterns = new[]
        {
            Enumerable.Range(0, 2000).Select(i => i.ToString()).ToArray(),
            Enumerable.Range(0, 2000).Reverse().Select(i => i.ToString()).ToArray(),
            Enumerable.Range(0, 2000).Select(i => (i % 7).ToString()).ToArray(),
            Enumerable.Range(0, 2000).Select(_ => random.Next(100).ToString()).ToArray()
        };

        foreach (var input in patterns)
        {
            var quick = new QuickSort().Sort(input);
            for (var i = 1; i < quick.Length; i++)
            {
                Assert.True(StringComparer.Ordinal.Compare(quick[i - 1], quick[i]) <= 0);
            }
        }
    }

    [Fact]
    public void FrequencyAnalyzer_CountsAndRanks()
    {
        var sorted = new[] { "apple", "apple", "banana", "data", "data", "data" };
        var report = WordFrequencyAnalyzer.Analyze(sorted);

        Assert.Equal(6, report.TotalWords);
        Assert.Equal(3, report.UniqueWords);
        Assert.Equal(new WordFrequency("apple", 2), report.Frequencies[0]);
        Assert.Equal(new WordFrequency("data", 3), report.Frequencies[2]);
        Assert.Equal("data", report.TopWords[0].Word);
        Assert.Equal(3, report.TopWords[0].Count);
    }

    [Fact]
    public void FrequencyAnalyzer_Empty()
    {
        var report = WordFrequencyAnalyzer.Analyze(Array.Empty<string>());
        Assert.Equal(0, report.TotalWords);
        Assert.Equal(0, report.UniqueWords);
        Assert.Empty(report.Frequencies);
        Assert.Empty(report.TopWords);
    }

    [Fact]
    public void TextGenerator_IsReproducible()
    {
        var first = TextGenerator.Generate(500, seed: 123, TextLanguage.Mixed);
        var second = TextGenerator.Generate(500, seed: 123, TextLanguage.Mixed);
        var other = TextGenerator.Generate(500, seed: 124, TextLanguage.Mixed);

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal(500, TextTokenizer.Tokenize(first).Count);
        Assert.Equal(500, TextTokenizer.Tokenize(other).Count);
    }

    [Fact]
    public void TextGenerator_RussianAndEnglish()
    {
        var english = TextTokenizer.Tokenize(TextGenerator.Generate(100, 5, TextLanguage.English));
        var russian = TextTokenizer.Tokenize(TextGenerator.Generate(100, 5, TextLanguage.Russian));

        Assert.Equal(100, english.Count);
        Assert.Equal(100, russian.Count);
        Assert.Contains(english, word => word.All(character => character < 128));
        Assert.Contains(russian, word => word.Any(character => character >= 'А' && character <= 'я'));
    }

    [Fact]
    public async Task BenchmarkRunner_ProducesRowsAndMatchesResults()
    {
        var progress = new List<BenchmarkProgress>();
        var report = await new BenchmarkRunner().RunAsync(
            new BenchmarkOptions(new[] { 50, 100, 200 }, Repeats: 2, WarmupRuns: 1, Seed: 42),
            new Progress<BenchmarkProgress>(progress.Add));

        Assert.Equal(3, report.Rows.Count);
        Assert.Equal(new[] { 50, 100, 200 }, report.Rows.Select(row => row.WordCount));
        Assert.All(report.Rows, row =>
        {
            Assert.True(row.QuickSortMedianMs >= 0);
            Assert.True(row.RadixSortMedianMs >= 0);
            Assert.Equal(2, row.Repeats);
        });
        Assert.NotEmpty(progress);
    }

    [Fact]
    public void LexicographicRule_IsOrdinalUtf16()
    {
        var words = new[] { "я", "а", "ё", "е", "a", "A" };
        // Ординальный порядок UTF-16: сначало латиница, затем кириллица по кодам.
        var expected = new[] { "A", "a", "а", "е", "я", "ё" };
        Assert.Equal(expected, new QuickSort().Sort(words));
        Assert.Equal(expected, new RadixSort().Sort(words));
    }
}
