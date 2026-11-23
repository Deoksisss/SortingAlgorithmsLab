using SortingLab.Core.Csv;
using SortingLab.Core.Models;

namespace SortingLab.Core.Abstractions;

public interface IExternalSortAlgorithm
{
    AlgorithmDescriptor Descriptor { get; }

    Task<ExternalSortResult> SortAsync(
        ExternalSortRequest request,
        IStepSink<ExternalSortStep> stepSink,
        CancellationToken cancellationToken = default);
}

public sealed record ExternalSortRequest(
    IReadOnlyList<string> Headers,
    IReadOnlyList<CsvRecord> Records,
    SortKey SortKey,
    string? WorkingRoot = null);
