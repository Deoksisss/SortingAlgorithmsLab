using SortingLab.Core.Abstractions;
using SortingLab.Core.Models;

namespace SortingLab.Core.Algorithms.Text;

/// <summary>
/// LSD-поразрядная сортировка строк по кодовым единицам UTF-16.
/// Виртуальный терминатор 0 обеспечивает корректный порядок строк
/// переменной длины и совпадение с ординальным сравнением.
/// </summary>
public sealed class RadixSort : ITextSortAlgorithm
{
    private const int BucketCount = 65536 + 1;

    public AlgorithmDescriptor Descriptor { get; } = new(
        Id: "radix-sort",
        Name: "Radix Sort",
        Category: "Поразрядная сортировка",
        ShortDescription: "Устойчивая LSD-сортировка строк по символам UTF-16, от последнего разряда к первому.",
        Idea: "Каждый символ строки — разряд. Строки дополняются виртуальным нулевым символом, затем устойчиво сортируются подсчётом по каждому разряду справа налево.",
        Principle: "Для каждой позиции символа строится массив частот на 65 537 значений и выполняется устойчивое распределение. После прохода по всем позициям строки упорядочены лексикографически.",
        TimeComplexity: "O(d · (n + 65536)), где d — максимальная длина слова",
        SpaceComplexity: "O(n + 65536) дополнительной памяти",
        Features: "Поддерживает Unicode и строки переменной длины, не использует сравнение строк на каждом шаге.");

    public string[] Sort(IReadOnlyList<string> words, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(words);
        var source = words.ToArray();
        if (source.Length <= 1)
        {
            return source;
        }

        var maxLength = 0;
        foreach (var word in source)
        {
            if (word.Length > maxLength)
            {
                maxLength = word.Length;
            }
        }

        if (maxLength == 0)
        {
            return source;
        }

        var buffer = new string[source.Length];
        var counts = new int[BucketCount];
        var current = source;

        for (var position = maxLength - 1; position >= 0; position--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Clear(counts, 0, counts.Length);

            foreach (var word in current)
            {
                var bucket = CharAt(word, position) + 1;
                counts[bucket]++;
            }

            for (var bucket = 1; bucket < counts.Length; bucket++)
            {
                counts[bucket] += counts[bucket - 1];
            }

            foreach (var word in current)
            {
                var code = CharAt(word, position);
                buffer[counts[code]++] = word;
            }

            (current, buffer) = (buffer, current);
        }

        return current;
    }

    private static int CharAt(string word, int position) =>
        position < word.Length ? word[position] : 0;
}
