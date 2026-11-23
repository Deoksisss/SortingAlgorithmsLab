using SortingLab.Core.Abstractions;
using SortingLab.Core.Models;

namespace SortingLab.Core.Algorithms.Text;

/// <summary>
/// Быстрая сортировка с 3-way partition, медианой из трёх и introsort-защитой.
/// </summary>
public sealed class QuickSort : ITextSortAlgorithm
{
    private const int InsertionThreshold = 16;
    private static readonly StringComparer Comparer = StringComparer.Ordinal;

    public AlgorithmDescriptor Descriptor { get; } = new(
        Id: "quick-sort",
        Name: "QuickSort",
        Category: "Усовершенствованный алгоритм",
        ShortDescription: "Быстрая сортировка с разбиением на три группы: меньше, равно и больше опорного элемента.",
        Idea: "Выбирается опорный элемент, массив делится на три части. Части меньше и больше опорного сортируются рекурсивно.",
        Principle: "Опорный элемент — медиана первого, среднего и последнего. Для дубликатов используется 3-way partition. Малые диапазоны сортируются вставками, при слишком глубокой рекурсии — собственной пирамидальной сортировкой.",
        TimeComplexity: "Среднее O(n log n); худшее O(n log n) благодаря introsort-защите",
        SpaceComplexity: "O(log n) на стек рекурсии",
        Features: "Эффективен на практике, устойчив к дубликатам и неудачным входам.");

    public string[] Sort(IReadOnlyList<string> words, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(words);
        var array = words.ToArray();
        if (array.Length <= 1)
        {
            return array;
        }

        var depthLimit = 2 * (int)Math.Ceiling(Math.Log2(array.Length));
        SortRange(array, 0, array.Length - 1, depthLimit, cancellationToken);
        return array;
    }

    private static void SortRange(
        string[] array,
        int low,
        int high,
        int depthLimit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (low < high)
        {
            if (high - low + 1 <= InsertionThreshold)
            {
                InsertionSort(array, low, high, cancellationToken);
                return;
            }

            if (depthLimit == 0)
            {
                HeapSort(array, low, high, cancellationToken);
                return;
            }

            depthLimit--;
            var pivot = MedianOfThree(array, low, high);
            var less = low;
            var current = low;
            var greater = high;

            while (current <= greater)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var comparison = Comparer.Compare(array[current], pivot);
                if (comparison < 0)
                {
                    Swap(array, less, current);
                    less++;
                    current++;
                }
                else if (comparison > 0)
                {
                    Swap(array, current, greater);
                    greater--;
                }
                else
                {
                    current++;
                }
            }

            // Рекурсия в меньшую часть, хвостовая обработка большей.
            if (less - low < high - greater)
            {
                SortRange(array, low, less - 1, depthLimit, cancellationToken);
                low = greater + 1;
            }
            else
            {
                SortRange(array, greater + 1, high, depthLimit, cancellationToken);
                high = less - 1;
            }
        }
    }

    private static string MedianOfThree(string[] array, int low, int high)
    {
        var middle = low + (high - low) / 2;
        if (Comparer.Compare(array[low], array[middle]) > 0)
        {
            Swap(array, low, middle);
        }

        if (Comparer.Compare(array[low], array[high]) > 0)
        {
            Swap(array, low, high);
        }

        if (Comparer.Compare(array[middle], array[high]) > 0)
        {
            Swap(array, middle, high);
        }

        return array[middle];
    }

    private static void InsertionSort(
        string[] array,
        int low,
        int high,
        CancellationToken cancellationToken)
    {
        for (var i = low + 1; i <= high; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = array[i];
            var j = i - 1;
            while (j >= low && Comparer.Compare(array[j], value) > 0)
            {
                array[j + 1] = array[j];
                j--;
            }

            array[j + 1] = value;
        }
    }

    private static void HeapSort(
        string[] array,
        int low,
        int high,
        CancellationToken cancellationToken)
    {
        var length = high - low + 1;
        for (var i = length / 2 - 1; i >= 0; i--)
        {
            SiftDown(array, low, i, length, cancellationToken);
        }

        for (var end = length - 1; end > 0; end--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Swap(array, low, low + end);
            SiftDown(array, low, 0, end, cancellationToken);
        }
    }

    private static void SiftDown(
        string[] array,
        int offset,
        int index,
        int length,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var child = index * 2 + 1;
            if (child >= length)
            {
                return;
            }

            if (child + 1 < length &&
                Comparer.Compare(array[offset + child], array[offset + child + 1]) < 0)
            {
                child++;
            }

            if (Comparer.Compare(array[offset + index], array[offset + child]) >= 0)
            {
                return;
            }

            Swap(array, offset + index, offset + child);
            index = child;
        }
    }

    private static void Swap(string[] array, int left, int right)
    {
        if (left == right)
        {
            return;
        }

        (array[left], array[right]) = (array[right], array[left]);
    }
}
