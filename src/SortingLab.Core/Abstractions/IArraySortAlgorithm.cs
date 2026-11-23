using SortingLab.Core.Models;

namespace SortingLab.Core.Abstractions;

public interface IStepSink<in TStep>
{
    ValueTask WriteAsync(TStep step, CancellationToken cancellationToken = default);
}

public interface IArraySortAlgorithm
{
    AlgorithmDescriptor Descriptor { get; }

    Task<ArraySortResult> SortAsync(
        IReadOnlyList<int> values,
        IStepSink<ArraySortStep> stepSink,
        CancellationToken cancellationToken = default);
}
