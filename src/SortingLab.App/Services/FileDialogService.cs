using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SortingLab.App.Services;

public sealed class FileDialogService
{
    private readonly Window _window;

    public FileDialogService(Window window)
    {
        _window = window;
    }

    public FilePickerFileType CsvType { get; } = new("CSV-файлы")
    {
        Patterns = new[] { "*.csv" },
        MimeTypes = new[] { "text/csv", "text/plain" }
    };

    public FilePickerFileType TextType { get; } = new("Текстовые файлы")
    {
        Patterns = new[] { "*.txt" },
        MimeTypes = new[] { "text/plain" }
    };

    public async Task<string?> OpenFileAsync(string title, FilePickerFileType type)
    {
        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { type, FilePickerFileTypes.All }
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SaveFileAsync(
        string title,
        string suggestedName,
        FilePickerFileType type)
    {
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = type.Patterns?.FirstOrDefault()?.TrimStart('*', '.'),
            FileTypeChoices = new[] { type }
        });

        return file?.TryGetLocalPath();
    }

    public void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{directory}\"",
            UseShellExecute = true
        });
    }
}
