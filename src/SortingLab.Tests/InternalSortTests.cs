using SortingLab.Core.Algorithms.Internal;
using SortingLab.Core.Models;

namespace SortingLab.Tests;

public sealed class InternalSortTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new[] { 5 } };
        yield return new object[] { new[] { 1, 2, 3, 4, 5 } };
        yield return new object[] { new[] { 5, 4, 3, 2, 1 } };
        yield return new object[] { new[] { 3, 1, 3, 2, 1, 3 } };
        yield return new object[] { new[] { -3, 0, -7, 5, -1, 5, -7 } };
        yield return new object[] { new[] { 0, 0, 0, 0, 0 } };
        yield return new object[] { new[] { 10, -10, 0, 10, -10, 0, 7, 7, 7 } };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task OddEvenSort_SortsCorrectly(int[] input)
    {
        var algorithm = new OddEvenSort();
        var (result, steps) = await SortTestHelper.RunAsync(algorithm, input);

        SortTestHelper.AssertSorted(result.SortedValues);
        SortTestHelper.AssertSameMultiset(input, result.SortedValues);
        Assert.Equal(input.Length, result.SortedValues.Length);
        Assert.NotEmpty(steps);
        Assert.Equal(1, steps[0].Number);
        Assert.True(steps[0].IsInitial);
        Assert.Equal(input, steps[0].Snapshot);
        Assert.True(steps[^1].IsCompleted);
        Assert.Equal(result.SortedValues, steps[^1].Snapshot);
        Assert.Equal(steps.Count, steps[^1].Number);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task TournamentSort_SortsCorrectly(int[] input)
    {
        var algorithm = new TournamentSort();
        var (result, steps) = await SortTestHelper.RunAsync(algorithm, input);

        SortTestHelper.AssertSorted(result.SortedValues);
        SortTestHelper.AssertSameMultiset(input, result.SortedValues);
        Assert.NotEmpty(steps);
        Assert.True(steps[0].IsInitial);
        Assert.True(steps[^1].IsCompleted);
        Assert.Equal(result.SortedValues, steps[^1].Snapshot);
        Assert.Equal(steps.Count, steps[^1].Number);
    }

    [Fact]
    public async Task OddEvenSort_ReverseHasSwaps()
    {
        var input = Enumerable.Range(1, 40).Reverse().ToArray();
        var (result, _) = await SortTestHelper.RunAsync(new OddEvenSort(), input);

        Assert.True(result.Statistics.Comparisons > 0);
        Assert.True(result.Statistics.Swaps > 0);
        Assert.True(result.Statistics.TotalOperations > 0);
    }

    [Fact]
    public async Task TournamentSort_ReverseMovesEveryElement()
    {
        var input = Enumerable.Range(1, 40).Reverse().ToArray();
        var (result, steps) = await SortTestHelper.RunAsync(new TournamentSort(), input);

        Assert.Equal(input.Length, result.Statistics.Moves);
        Assert.True(result.Statistics.Comparisons > 0);
        Assert.Contains(steps, step => step.Tournament is not null);
        Assert.Contains(steps, step => step.Kind == ArrayStepKind.Select);
        Assert.Contains(steps, step => step.Kind == ArrayStepKind.TreeUpdated);
    }

    [Fact]
    public async Task TournamentTree_RootIsMinimumOnFirstSnapshot()
    {
        var input = new[] { 9, 4, 7, 1, 8, 2, 6, 3 };
        var (_, steps) = await SortTestHelper.RunAsync(new TournamentSort(), input);
        var tree = steps.First(step => step.Title == "Дерево построено").Tournament!;

        Assert.Equal(3, tree.WinnerIndex);
        Assert.Equal(1, tree.Nodes[1].Value);
    }

    [Fact]
    public async Task LargeRandomArrays_AreSorted()
    {
        var random = new Random(12345);
        var input = Enumerable.Range(0, 200).Select(_ => random.Next(-500, 500)).ToArray();

        foreach (var algorithm in new Core.Abstractions.IArraySortAlgorithm[] { new OddEvenSort(), new TournamentSort() })
        {
            var (result, _) = await SortTestHelper.RunAsync(algorithm, input);
            SortTestHelper.AssertSorted(result.SortedValues);
            SortTestHelper.AssertSameMultiset(input, result.SortedValues);
        }
    }

    [Fact]
    public async Task Cancellation_StopsAlgorithm()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new OddEvenSort().SortAsync(new[] { 3, 2, 1 }, new Core.Execution.CollectingStepSink<ArraySortStep>(), source.Token));
    }

    [Fact]
    public async Task AlwaysEqualArray_Completes()
    {
        var input = Enumerable.Repeat(42, 60).ToArray();
        var (result, _) = await SortTestHelper.RunAsync(new OddEvenSort(), input);

        Assert.All(result.SortedValues, value => Assert.Equal(42, value));
        Assert.Equal(0, result.Statistics.Swaps);
    }
}
