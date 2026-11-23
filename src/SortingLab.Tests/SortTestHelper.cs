using SortingLab.Core.Abstractions;
using SortingLab.Core.Execution;
using SortingLab.Core.Models;

namespace SortingLab.Tests;

internal static class SortTestHelper
{
    public static async Task<(ArraySortResult Result, IReadOnlyList<ArraySortStep> Steps)> RunAsync(
        IArraySortAlgorithm algorithm,
        IReadOnlyList<int> input)
    {
        var sink = new CollectingStepSink<ArraySortStep>();
        var result = await algorithm.SortAsync(input, sink);
        return (result, sink.Steps);
    }

    public static void AssertSorted(IReadOnlyList<int> values)
    {
        for (var i = 1; i < values.Count; i++)
        {
            Assert.True(values[i - 1] <= values[i], $"Нарушен порядок на позиции {i}: {values[i - 1]} > {values[i]}.");
        }
    }

    public static void AssertSameMultiset(IReadOnlyList<int> expected, IReadOnlyList<int> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        var expectedSorted = expected.OrderBy(value => value).ToArray();
        var actualSorted = actual.OrderBy(value => value).ToArray();
        Assert.Equal(expectedSorted, actualSorted);
    }
}
