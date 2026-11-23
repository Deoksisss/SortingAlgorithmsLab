using System.Text;
using SortingLab.Core.Models;

namespace SortingLab.Core.Csv;

public static class CsvTableWriter
{
    private const char Delimiter = ',';

    public static string WriteToString(CsvTable table)
    {
        var builder = new StringBuilder();
        Write(new StringWriter(builder), table);
        return builder.ToString();
    }

    public static void WriteFile(CsvTable table, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Не указан путь для сохранения файла.", nameof(path));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Папка для сохранения не найдена: {directory}");
        }

        var tempPath = Path.Combine(directory, $".sortinglab-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            {
                Write(writer, table);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Исходная ошибка важнее ошибки удаления временного файла.
                }
            }

            throw;
        }
    }

    public static void Write(TextWriter writer, CsvTable table)
    {
        WriteRow(writer, table.Headers);
        foreach (var record in table.Records)
        {
            var fields = new string[table.Headers.Count];
            for (var i = 0; i < fields.Length; i++)
            {
                fields[i] = record.Field(i);
            }

            WriteRow(writer, fields);
        }
    }

    private static void WriteRow(TextWriter writer, IReadOnlyList<string> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(Delimiter);
            }

            writer.Write(Escape(fields[i]));
        }

        writer.WriteLine();
    }

    private static string Escape(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var requiresQuotes = value.IndexOfAny(new[] { Delimiter, '"', '\r', '\n' }) >= 0;
        if (!requiresQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
