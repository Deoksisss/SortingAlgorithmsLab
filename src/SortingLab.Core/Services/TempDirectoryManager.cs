namespace SortingLab.Core.Services;

/// <summary>
/// Создание и очистка временных каталогов внешней сортировки.
/// </summary>
public static class TempDirectoryManager
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "SortingAlgorithmsLab");

    public static string CreateSessionDirectory(string? workingRoot = null)
    {
        var root = string.IsNullOrWhiteSpace(workingRoot) ? Root : workingRoot;
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"sort-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static void TryDelete(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Временный каталог не должен скрывать исходную ошибку операции.
        }
    }

    /// <summary>
    /// Удаляет каталоги прошлых сессий, которым больше суток.
    /// </summary>
    public static void CleanupStaleDirectories(TimeSpan? olderThan = null)
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        var threshold = DateTime.UtcNow - (olderThan ?? TimeSpan.FromDays(1));
        foreach (var directory in Directory.EnumerateDirectories(Root))
        {
            try
            {
                var info = new DirectoryInfo(directory);
                if (info.LastWriteTimeUtc < threshold)
                {
                    info.Delete(recursive: true);
                }
            }
            catch
            {
                // Каталог может использоваться другим экземпляром приложения.
            }
        }
    }
}
