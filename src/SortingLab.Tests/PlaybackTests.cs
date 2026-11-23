using SortingLab.App.Services;
using SortingLab.App.ViewModels.Common;
using SortingLab.Core.Execution;
using SortingLab.Core.Models;

namespace SortingLab.Tests;

public sealed class PlaybackTests
{
    [Fact]
    public async Task Start_RunsToCompletion_AndBuildsLog()
    {
        var applied = 0;
        var resetCount = 0;
        var channel = new StepChannel<DemoStep>();
        var producer = ProduceAsync(channel, 4);
        var playback = CreatePlayback(channel, () => applied++, () => resetCount++, producer);
        playback.StepDelayMilliseconds = 0;

        await playback.StartCommand.ExecuteAsync(null);

        Assert.Equal(DemoPlaybackState.Completed, playback.State);
        Assert.Equal(5, playback.StepCount);
        Assert.Equal(5, applied);
        Assert.Equal(5, playback.Log.Count);
        Assert.True(playback.IsCompleted);
        Assert.NotNull(playback.Statistics);
    }

    [Fact]
    public async Task ManualStep_FromIdle_AppliesInitialAndOneOperation()
    {
        var channel = new StepChannel<DemoStep>();
        var producer = ProduceAsync(channel, 8);
        var playback = CreatePlayback(channel, () => { }, () => { }, producer);

        Assert.True(playback.StepCommand.CanExecute(null));
        playback.StepCommand.Execute(null);
        await Task.Delay(300);
        Assert.True(
            playback.StepCount >= 2,
            $"state={playback.State}, step={playback.StepCount}, log={playback.Log.Count}, canStep={playback.StepCommand.CanExecute(null)}");
        Assert.Equal(DemoPlaybackState.Paused, playback.State);
        Assert.Equal(2, playback.StepCount);

        playback.StepCommand.Execute(null);
        await WaitUntilAsync(() => playback.StepCount >= 3);
        Assert.Equal(DemoPlaybackState.Paused, playback.State);
        Assert.Equal(3, playback.StepCount);

        playback.StopCommand.Execute(null);
        await WaitUntilAsync(() => playback.State == DemoPlaybackState.Cancelled);
        Assert.True(playback.IsCancelled);
    }

    [Fact]
    public async Task PauseThenResume_ChangesState()
    {
        var channel = new StepChannel<DemoStep>();
        var producer = ProduceAsync(channel, 20);
        var playback = CreatePlayback(channel, () => { }, () => { }, producer);
        playback.StepDelayMilliseconds = 200;
        _ = playback.StartCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => playback.StepCount >= 1);
        playback.PauseCommand.Execute(null);
        Assert.Equal(DemoPlaybackState.Paused, playback.State);
        Assert.True(playback.IsPaused);

        playback.ResumeCommand.Execute(null);
        Assert.Equal(DemoPlaybackState.Running, playback.State);
        playback.StopCommand.Execute(null);
        await WaitUntilAsync(() => playback.State == DemoPlaybackState.Cancelled);
    }

    [Fact]
    public async Task Error_IsExposedAndStateIsFaulted()
    {
        var channel = new StepChannel<DemoStep>();
        channel.Complete(new InvalidOperationException("Тестовая ошибка"));
        var playback = CreatePlayback(channel, () => { }, () => { }, Task.CompletedTask);
        playback.StepDelayMilliseconds = 0;

        await playback.StartCommand.ExecuteAsync(null);

        Assert.Equal(DemoPlaybackState.Faulted, playback.State);
        Assert.True(playback.HasError);
        Assert.Contains("Тестовая ошибка", playback.ErrorMessage);
    }

    [Fact]
    public async Task Reset_ClearsLogAndReturnsToIdle()
    {
        var resetCount = 0;
        var channel = new StepChannel<DemoStep>();
        var producer = ProduceAsync(channel, 3);
        var playback = CreatePlayback(channel, () => { }, () => resetCount++, producer);
        playback.StepDelayMilliseconds = 0;
        await playback.StartCommand.ExecuteAsync(null);

        await playback.ResetCommand.ExecuteAsync(null);

        Assert.Equal(DemoPlaybackState.Idle, playback.State);
        Assert.Empty(playback.Log);
        Assert.Equal(0, playback.StepCount);
        Assert.Equal(1, resetCount);
    }

    private static PlaybackViewModel CreatePlayback(
        StepChannel<DemoStep> channel,
        Action apply,
        Action reset,
        Task producer)
    {
        var playback = new PlaybackViewModel();
        playback.Configure(
            _ => new DemonstrationStream(channel, producer),
            _ => apply(),
            reset);
        return playback;
    }

    private static async Task ProduceAsync(StepChannel<DemoStep> channel, int count)
    {
        try
        {
            await channel.WriteAsync(new TestStep
            {
                Number = 1,
                Title = "Исходные данные",
                Explanation = "Старт",
                Initial = true
            });
            for (var i = 0; i < count; i++)
            {
                await channel.WriteAsync(new TestStep
                {
                    Number = i + 2,
                    Title = $"Шаг {i + 1}",
                    Explanation = "Операция"
                });
            }

            channel.Complete();
        }
        catch (Exception exception)
        {
            channel.Complete(exception);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Условие не выполнилось за отведённое время.");
            }

            await Task.Delay(10);
        }
    }

    private sealed record TestStep : DemoStep
    {
        public bool Initial { get; init; }

        public override bool IsInitial => Initial;

        public override string KindLabel => "Тест";

        public override string Details => string.Empty;
    }
}
