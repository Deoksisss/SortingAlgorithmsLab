namespace SortingLab.Core.Text;

public sealed record WordFrequency(string Word, int Count);

public sealed record WordFrequencyReport(
    IReadOnlyList<WordFrequency> Frequencies,
    int TotalWords,
    int UniqueWords,
    IReadOnlyList<WordFrequency> TopWords);

/// <summary>
/// Подсчёт частот по уже отсортированному массиву: одинаковые слова стоят рядом.
/// </summary>
public static class WordFrequencyAnalyzer
{
    public static WordFrequencyReport Analyze(IReadOnlyList<string> sortedWords)
    {
        ArgumentNullException.ThrowIfNull(sortedWords);
        if (sortedWords.Count == 0)
        {
            return new WordFrequencyReport(
                Array.Empty<WordFrequency>(),
                0,
                0,
                Array.Empty<WordFrequency>());
        }

        var frequencies = new List<WordFrequency>();
        var current = sortedWords[0];
        var count = 1;

        for (var i = 1; i < sortedWords.Count; i++)
        {
            if (string.Equals(sortedWords[i], current, StringComparison.Ordinal))
            {
                count++;
            }
            else
            {
                frequencies.Add(new WordFrequency(current, count));
                current = sortedWords[i];
                count = 1;
            }
        }

        frequencies.Add(new WordFrequency(current, count));

        var top = frequencies
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Word, StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        return new WordFrequencyReport(
            frequencies,
            sortedWords.Count,
            frequencies.Count,
            top);
    }
}
