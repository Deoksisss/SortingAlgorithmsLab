namespace SortingLab.Core.Models;

public sealed class CsvRecord
{
    public CsvRecord(int id, IReadOnlyList<string> fields)
    {
        Id = id;
        Fields = fields;
    }

    public int Id { get; }

    public IReadOnlyList<string> Fields { get; }

    public string Field(int index) => index >= 0 && index < Fields.Count ? Fields[index] : string.Empty;
}

public sealed class CsvTable
{
    public CsvTable(IReadOnlyList<string> headers, IReadOnlyList<CsvRecord> records, string sourceName = "")
    {
        Headers = headers;
        Records = records;
        SourceName = sourceName;
    }

    public IReadOnlyList<string> Headers { get; }

    public IReadOnlyList<CsvRecord> Records { get; }

    public string SourceName { get; }

    public int ColumnCount => Headers.Count;

    public CsvTable WithRecords(IReadOnlyList<CsvRecord> records) =>
        new(Headers, records, SourceName);
}
