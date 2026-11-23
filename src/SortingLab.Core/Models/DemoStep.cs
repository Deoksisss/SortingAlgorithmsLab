namespace SortingLab.Core.Models;

/// <summary>
/// Базовая модель шага любого алгоритма. UI использует только эти поля
/// для журнала и общих панелей, а конкретные типы шагов — для визуализации.
/// </summary>
public abstract record DemoStep
{
    public int Number { get; init; }

    public required string Title { get; init; }

    public required string Explanation { get; init; }

    public OperationStatistics Statistics { get; init; } = OperationStatistics.Empty;

    public abstract string KindLabel { get; }

    public abstract string Details { get; }

    public virtual bool IsInitial => false;

    public virtual bool IsCompleted => false;
}
