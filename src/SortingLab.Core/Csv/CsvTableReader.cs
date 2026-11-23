using System.Text;
using SortingLab.Core.Models;

namespace SortingLab.Core.Csv;

public sealed class CsvFormatException : Exception
{
    public CsvFormatException(string message) : base(message)
    {
    }

    public CsvFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed record CsvParseResult(
    CsvTable Table,
    char Delimiter,
    IReadOnlyList<string> Warnings);

public static class CsvTableReader
{
    private const long MaxFileSizeBytes = 64L * 1024 * 1024;
    private static readonly char[] CandidateDelimiters = { ',', ';', '\t', '|' };

    public static CsvParseResult ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new CsvFormatException($"Файл не найден: {path}");
        }

        var info = new FileInfo(path);
        if (info.Length > MaxFileSizeBytes)
        {
            throw new CsvFormatException(
                $"Файл слишком большой ({info.Length / (1024.0 * 1024.0):0.0} МБ). " +
                "Максимальный размер для загрузки — 64 МБ.");
        }

        try
        {
            using var reader = new StreamReader(
                path,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false),
                detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return Parse(text, Path.GetFileName(path));
        }
        catch (CsvFormatException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new CsvFormatException($"Не удалось прочитать файл «{Path.GetFileName(path)}»: {exception.Message}", exception);
        }
    }

    public static CsvParseResult Parse(string text, string sourceName = "")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new CsvFormatException("Файл пуст: нет ни заголовков, ни данных.");
        }

        if (text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        var delimiter = DetectDelimiter(text);
        var rows = ParseRows(text, delimiter, out var warnings);
        if (rows.Count == 0)
        {
            throw new CsvFormatException("В файле не найдено ни одной строки с данными.");
        }

        var headers = rows[0].Select(NormalizeHeader).ToList();
        var duplicateHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            if (string.IsNullOrWhiteSpace(header))
            {
                header = $"Столбец {i + 1}";
                warnings.Add($"Пустой заголовок столбца {i + 1} заменён на «{header}».");
            }

            var unique = header;
            var suffix = 2;
            while (!duplicateHeaders.Add(unique))
            {
                unique = $"{header} ({suffix})";
                suffix++;
            }

            if (!string.Equals(unique, header, StringComparison.Ordinal))
            {
                warnings.Add($"Повторяющийся заголовок «{header}» заменён на «{unique}».");
            }

            headers[i] = unique;
        }

        var records = new List<CsvRecord>();
        var id = 0;
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var fields = rows[rowIndex];
            if (fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0]))
            {
                continue;
            }

            if (fields.Count < headers.Count)
            {
                warnings.Add($"Строка {rowIndex + 1}: не хватает {headers.Count - fields.Count} значений; они заполнены пустыми.");
                while (fields.Count < headers.Count)
                {
                    fields.Add(string.Empty);
                }
            }
            else if (fields.Count > headers.Count)
            {
                warnings.Add($"Строка {rowIndex + 1}: значений больше, чем столбцов; лишние значения пропущены.");
                fields = fields.Take(headers.Count).ToList();
            }

            records.Add(new CsvRecord(id++, fields));
        }

        if (warnings.Count > 50)
        {
            var hidden = warnings.Count - 50;
            warnings.RemoveRange(50, warnings.Count - 50);
            warnings.Add($"…и ещё {hidden} предупреждений.");
        }

        return new CsvParseResult(
            new CsvTable(headers, records, sourceName),
            delimiter,
            warnings);
    }

    private static string NormalizeHeader(string header) => header.Trim();

    private static char DetectDelimiter(string text)
    {
        var scores = CandidateDelimiters.ToDictionary(delimiter => delimiter, _ => 0);
        var inQuotes = false;
        var scannedLines = 0;
        var lineHasContent = false;

        for (var i = 0; i < text.Length && scannedLines < 10; i++)
        {
            var character = text[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                lineHasContent = true;
                continue;
            }

            if (!inQuotes && CandidateDelimiters.Contains(character))
            {
                scores[character]++;
                lineHasContent = true;
                continue;
            }

            if (!inQuotes && (character == '\r' || character == '\n'))
            {
                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                if (lineHasContent)
                {
                    scannedLines++;
                }

                lineHasContent = false;
            }
        }

        var best = CandidateDelimiters
            .OrderByDescending(delimiter => scores[delimiter])
            .First();

        return scores[best] == 0 ? ',' : best;
    }

    private static List<List<string>> ParseRows(
        string text,
        char delimiter,
        out List<string> warnings)
    {
        warnings = new List<string>();
        var rows = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && !fieldStarted)
            {
                inQuotes = true;
                fieldStarted = true;
                continue;
            }

            if (character == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                continue;
            }

            if (character == '\r' || character == '\n')
            {
                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                fields.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                if (fields.Count > 1 || !string.IsNullOrWhiteSpace(fields[0]))
                {
                    rows.Add(fields);
                }

                fields = new List<string>();
                continue;
            }

            field.Append(character);
            fieldStarted = true;
        }

        if (inQuotes)
        {
            warnings.Add("Файл заканчивается внутри кавычек: последнее поле прочитано до конца файла.");
        }

        if (field.Length > 0 || fields.Count > 0 || fieldStarted)
        {
            fields.Add(field.ToString());
            if (fields.Count > 1 || !string.IsNullOrWhiteSpace(fields[0]))
            {
                rows.Add(fields);
            }
        }

        return rows;
    }
}
