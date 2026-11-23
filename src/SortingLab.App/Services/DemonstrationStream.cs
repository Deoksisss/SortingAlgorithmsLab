using SortingLab.Core.Models;

namespace SortingLab.App.Services;

public sealed record DemonstrationStream(
    IAsyncEnumerable<DemoStep> Steps,
    Task Completion);
