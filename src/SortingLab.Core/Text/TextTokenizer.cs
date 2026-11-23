using System.Text;

namespace SortingLab.Core.Text;

/// <summary>
/// Единые правила извлечения слов для QuickSort и Radix Sort.
/// </summary>
public static class TextTokenizer
{
    public static IReadOnlyList<string> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    public static string NormalizeWord(string word) =>
        word.Normalize(NormalizationForm.FormC).ToLowerInvariant();
}
