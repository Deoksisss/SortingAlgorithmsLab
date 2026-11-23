namespace SortingLab.Core.Models;

public sealed record ExternalRunInfo(
    int Number,
    int StartIndex,
    int EndIndex,
    int Count,
    string FirstKey,
    string LastKey,
    IReadOnlyList<int> PreviewRecordIds);

public enum ExternalStepKind
{
    Initial,
    Scan,
    RunDetected,
    RunsCompleted,
    MergeStarted,
    Compare,
    Select,
    WriteRecord,
    FileRead,
    FileWrite,
    MergeCompleted,
    PassCompleted,
    Completed
}

public sealed record ExternalSortStep : DemoStep
{
    public ExternalStepKind Kind { get; init; }

    public IReadOnlyList<ExternalRunInfo>? Runs { get; init; }

    public int? PassNumber { get; init; }

    public int? LeftRunNumber { get; init; }

    public int? RightRunNumber { get; init; }

    public int? LeftRecordId { get; init; }

    public int? RightRecordId { get; init; }

    public int? WrittenRecordId { get; init; }

    public string? LeftKey { get; init; }

    public string? RightKey { get; init; }

    public string? FileName { get; init; }

    public int? OutputIndex { get; init; }

    public IReadOnlyList<int>? FinalRecordIds { get; init; }

    public override bool IsInitial => Kind == ExternalStepKind.Initial;

    public override bool IsCompleted => Kind == ExternalStepKind.Completed;

    public override string KindLabel => Kind switch
    {
        ExternalStepKind.Initial => "Исходные данные",
        ExternalStepKind.Scan => "Поиск серий",
        ExternalStepKind.RunDetected => "Серия найдена",
        ExternalStepKind.RunsCompleted => "Серии определены",
        ExternalStepKind.MergeStarted => "Начало слияния",
        ExternalStepKind.Compare => "Сравнение",
        ExternalStepKind.Select => "Выбор записи",
        ExternalStepKind.WriteRecord => "Запись результата",
        ExternalStepKind.FileRead => "Чтение файла",
        ExternalStepKind.FileWrite => "Запись файла",
        ExternalStepKind.MergeCompleted => "Слияние завершено",
        ExternalStepKind.PassCompleted => "Проход завершён",
        ExternalStepKind.Completed => "Сортировка завершена",
        _ => "Операция"
    };

    public override string Details
    {
        get
        {
            if (Kind is ExternalStepKind.Compare or ExternalStepKind.Select)
            {
                return $"Слева: {LeftKey ?? "—"}, справа: {RightKey ?? "—"}";
            }

            if (WrittenRecordId is not null)
            {
                return $"Запись #{WrittenRecordId} → результат[{OutputIndex ?? 0}]";
            }

            if (Runs is not null)
            {
                return $"Серий: {Runs.Count}; проход: {PassNumber ?? 0}";
            }

            return FileName is null ? string.Empty : $"Файл: {FileName}";
        }
    }
}

public sealed record ExternalSortResult(
    IReadOnlyList<CsvRecord> SortedRecords,
    OperationStatistics Statistics,
    int InitialRunCount,
    int PassCount,
    string TempDirectory);
