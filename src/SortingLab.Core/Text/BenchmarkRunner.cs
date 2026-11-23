using System.Diagnostics;
using SortingLab.Core.Algorithms.Text;

namespace SortingLab.Core.Text;

public sealed record BenchmarkOptions(
    IReadOnlyList<int> Sizes,
    int Repeats = 5,
    int WarmupRuns = 1,
    int Seed = 2024,
    TextLanguage Language = TextLanguage.English);

public sealed record BenchmarkRow(
    int WordCount,
    double QuickSortMedianMs,
    double RadixSortMedianMs,
    double QuickSortMinMs,
    double RadixSortMinMs,
    int Repeats);

public sealed record BenchmarkReport(
    IReadOnlyList<BenchmarkRow> Rows,
    int Seed,
    int Repeats,
    DateTimeOffset CompletedAt);

public sealed record BenchmarkProgress(
    int CompletedSizes,
    int TotalSizes,
    int WordCount,
    string Stage);

/// <summary>
/// Замеры QuickSort и Radix Sort на одинаковых данных.
/// Генерация, нормализация и копирование исходного массива не входят в замер.
/// </summary>
public sealed class BenchmarkRunner
{
    private readonly QuickSort _quickSort = new();
    private readonly RadixSort _radixSort = new();

    public async Task<BenchmarkReport> RunAsync(
        BenchmarkOptions options,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var sizes = options.Sizes
            .Where(size => size > 0)
            .Distinct()
            .OrderBy(size => size)
            .ToArray();

        if (sizes.Length == 0)
        {
            throw new ArgumentException("Не задан ни один положительный размер текста.", nameof(options));
        }

        var repeats = Math.Max(1, options.Repeats);
        var warmups = Math.Max(0, options.WarmupRuns);
        var rows = new List<BenchmarkRow>(sizes.Length);

        for (var sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = sizes[sizeIndex];
            progress?.Report(new BenchmarkProgress(sizeIndex, sizes.Length, size, "Генерация текста"));

            var text = TextGenerator.Generate(size, options.Seed + size, options.Language);
            var words = TextTokenizer.Tokenize(text).ToArray();
            if (words.Length != size)
            {
                throw new InvalidOperationException(
                    $"Внутренняя ошибка генерации: запрошено {size} слов, получено {words.Length}.");
            }

            for (var warmup = 0; warmup < warmups; warmup++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _quickSort.Sort((string[])words.Clone(), cancellationToken);
                _radixSort.Sort((string[])words.Clone(), cancellationToken);
            }

            var quickTimes = new double[repeats];
            var radixTimes = new double[repeats];
            var stopwatch = new Stopwatch();

            for (var repeat = 0; repeat < repeats; repeat++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new BenchmarkProgress(
                    sizeIndex,
                    sizes.Length,
                    size,
                    $"Замер {repeat + 1} из {repeats}"));

                var quickInput = (string[])words.Clone();
                stopwatch.Restart();
                var quickResult = _quickSort.Sort(quickInput, cancellationToken);
                stopwatch.Stop();
                quickTimes[repeat] = stopwatch.Elapsed.TotalMilliseconds;

                var radixInput = (string[])words.Clone();
                stopwatch.Restart();
                var radixResult = _radixSort.Sort(radixInput, cancellationToken);
                stopwatch.Stop();
                radixTimes[repeat] = stopwatch.Elapsed.TotalMilliseconds;

                if (!quickResult.SequenceEqual(radixResult, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "QuickSort и Radix Sort дали разные результаты. Замеры остановлены.");
                }

                if (!IsSorted(quickResult))
                {
                    throw new InvalidOperationException("Результат сортировки не упорядочен.");
                }
            }

            rows.Add(new BenchmarkRow(
                size,
                Median(quickTimes),
                Median(radixTimes),
                quickTimes.Min(),
                radixTimes.Min(),
                repeats));

            progress?.Report(new BenchmarkProgress(
                sizeIndex + 1,
                sizes.Length,
                size,
                "Готово"));
            await Task.Yield();
        }

        return new BenchmarkReport(rows, options.Seed, repeats, DateTimeOffset.Now);
    }

    private static bool IsSorted(IReadOnlyList<string> words)
    {
        for (var i = 1; i < words.Count; i++)
        {
            if (StringComparer.Ordinal.Compare(words[i - 1], words[i]) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static double Median(double[] values)
    {
        var copy = (double[])values.Clone();
        Array.Sort(copy);
        var middle = copy.Length / 2;
        return copy.Length % 2 == 0
            ? (copy[middle - 1] + copy[middle]) / 2
            : copy[middle];
    }
}
