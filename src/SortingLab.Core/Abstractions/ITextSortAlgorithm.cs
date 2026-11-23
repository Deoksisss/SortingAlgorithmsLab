using SortingLab.Core.Models;

namespace SortingLab.Core.Abstractions;

public interface ITextSortAlgorithm
{
    AlgorithmDescriptor Descriptor { get; }

    string[] Sort(IReadOnlyList<string> words, CancellationToken cancellationToken = default);
}
