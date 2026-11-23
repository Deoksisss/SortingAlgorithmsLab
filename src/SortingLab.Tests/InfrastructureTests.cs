using SortingLab.Core.Execution;
using SortingLab.Core.Models;

namespace SortingLab.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task StepChannel_DeliversStepsInOrder()
    {
        var channel = new StepChannel<DemoStep>();
        var first = new TestStep { Number = 1, Title = "Первый", Explanation = "1" };
        var second = new TestStep { Number = 2, Title = "Второй", Explanation = "2" };

        await channel.WriteAsync(first);
        await channel.WriteAsync(second);
        channel.Complete();

        var steps = new List<DemoStep>();
        await foreach (var step in channel)
        {
            steps.Add(step);
        }

        Assert.Equal(new[] { 1, 2 }, steps.Select(step => step.Number));
    }

    [Fact]
    public async Task StepChannel_PropagatesError()
    {
        var channel = new StepChannel<DemoStep>();
        await channel.WriteAsync(new TestStep { Number = 1, Title = "Шаг", Explanation = "..." });
        channel.Complete(new InvalidOperationException("Ошибка алгоритма"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in channel)
            {
            }
        });
    }

    [Fact]
    public async Task CollectingSink_RespectsCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var sink = new CollectingStepSink<DemoStep>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sink.WriteAsync(new TestStep { Number = 1, Title = "Шаг", Explanation = "..." }, source.Token).AsTask());
    }

    [Fact]
    public async Task StepNumbers_AreSequentialInArrayAlgorithm()
    {
        var sink = new CollectingStepSink<ArraySortStep>();
        var context = new ArraySortContext(new[] { 2, 1 }, sink);
        await context.InitialAsync();
        await context.CompareAsync(0, 1, "Сравнение");
        await context.SwapAsync(0, 1, "Обмен");
        await context.CompleteAsync(new[] { 1, 2 }, "Готово");

        Assert.Equal(new[] { 1, 2, 3, 4 }, sink.Steps.Select(step => step.Number));
        Assert.Equal(new[] { 1, 2 }, sink.Steps[^1].Snapshot);
    }

    private sealed record TestStep : DemoStep
    {
        public override string KindLabel => "Тест";

        public override string Details => string.Empty;
    }
}
