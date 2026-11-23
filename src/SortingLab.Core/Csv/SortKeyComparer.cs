using System.Globalization;
using SortingLab.Core.Models;

namespace SortingLab.Core.Csv;

public enum SortKeyType
{
    Text,
    Integer,
    Decimal
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record SortKey(int ColumnIndex, SortKeyType Type, SortDirection Direction);

public sealed record CsvValidationError(int RecordId, int ColumnIndex, string Message);

/// <summary>
/// Сравнение записей по выбранному ключу. При равенстве ключей возвращает 0,
/// чтобы слияние сохраняло исходный порядок (устойчивость).
/// </summary>
public sealed class SortKeyComparer
{
    private static readonly StringComparer TextComparer = StringComparer.InvariantCultureIgnoreCase;
    private readonly int _columnIndex;
    private readonly SortKeyType _type;
    private readonly SortDirection _direction;

    public SortKeyComparer(int columnIndex, SortKeyType type, SortDirection direction)
    {
        if (columnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        _columnIndex = columnIndex;
        _type = type;
        _direction = direction;
    }

    public int ColumnIndex => _columnIndex;

    public int Compare(CsvRecord left, CsvRecord right)
    {
        var leftKey = ParseKey(left.Field(_columnIndex));
        var rightKey = ParseKey(right.Field(_columnIndex));

        int comparison;
        if (!leftKey.HasValue && !rightKey.HasValue)
        {
            comparison = 0;
        }
        else if (!leftKey.HasValue)
        {
            comparison = -1;
        }
        else if (!rightKey.HasValue)
        {
            comparison = 1;
        }
        else
        {
            comparison = _type switch
            {
                SortKeyType.Text => TextComparer.Compare(leftKey.Text, rightKey.Text),
                SortKeyType.Integer => leftKey.Integer.CompareTo(rightKey.Integer),
                SortKeyType.Decimal => leftKey.Decimal.CompareTo(rightKey.Decimal),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return _direction == SortDirection.Descending ? -comparison : comparison;
    }

    public string FormatKey(CsvRecord record)
    {
        var raw = record.Field(_columnIndex);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "—";
        }

        var key = ParseKey(raw);
        if (!key.HasValue)
        {
            return raw.Trim();
        }

        return _type switch
        {
            SortKeyType.Integer => key.Integer.ToString("N0", CultureInfo.InvariantCulture),
            SortKeyType.Decimal => key.Decimal.ToString("0.###", CultureInfo.InvariantCulture),
            _ => key.Text
        };
    }

    public IReadOnlyList<CsvValidationError> Validate(IEnumerable<CsvRecord> records)
    {
        var errors = new List<CsvValidationError>();
        foreach (var record in records)
        {
            var raw = record.Field(_columnIndex);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!TryParse(raw, out _))
            {
                errors.Add(new CsvValidationError(
                    record.Id,
                    _columnIndex,
                    _type == SortKeyType.Integer
                        ? $"Значение «{raw}» не является целым числом."
                        : $"Значение «{raw}» не является числом."));
            }
        }

        return errors;
    }

    private bool TryParse(string raw, out CsvKey key)
    {
        key = default;
        var text = raw.Trim();
        if (text.Length == 0)
        {
            return true;
        }

        if (_type == SortKeyType.Text)
        {
            key = new CsvKey(true, text, 0, 0);
            return true;
        }

        if (_type == SortKeyType.Integer)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                key = new CsvKey(true, text, integer, integer);
                return true;
            }

            return false;
        }

        var normalized = text.Replace('\u00A0', ' ').Trim();

        // В русскоязычных CSV десятичный разделитель часто запятая.
        // Одиночная запятая трактуется как десятичный разделитель, а не как разделитель тысяч.
        if (normalized.Contains(',') && !normalized.Contains('.'))
        {
            normalized = normalized.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var commaNumber))
            {
                key = new CsvKey(true, text, 0, commaNumber);
                return true;
            }
        }

        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;
        if (double.TryParse(normalized, styles, CultureInfo.InvariantCulture, out var number))
        {
            key = new CsvKey(true, text, 0, number);
            return true;
        }

        return false;
    }

    private CsvKey ParseKey(string raw) =>
        TryParse(raw, out var key) ? key : default;

    private readonly record struct CsvKey(bool HasValue, string Text, long Integer, double Decimal);
}
