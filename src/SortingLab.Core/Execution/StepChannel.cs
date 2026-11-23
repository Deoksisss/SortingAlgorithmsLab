using System.Threading.Channels;
using SortingLab.Core.Abstractions;

namespace SortingLab.Core.Execution;

/// <summary>
/// Поток шагов алгоритма. Неограниченный канал выбран осознанно:
/// демонстрационные объёмы ограничены, а пауза UI не должна искажать
/// время выполнения алгоритма.
/// </summary>
public sealed class StepChannel<TStep> : IStepSink<TStep>, IAsyncEnumerable<TStep>
{
    private readonly Channel<TStep> _channel = Channel.CreateUnbounded<TStep>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

    public ValueTask WriteAsync(TStep step, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(step, cancellationToken);

    public void Complete(Exception? error = null) =>
        _channel.Writer.TryComplete(error);

    public IAsyncEnumerator<TStep> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
}
