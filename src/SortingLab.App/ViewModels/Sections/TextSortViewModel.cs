using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Services;
using SortingLab.App.ViewModels.Common;
using SortingLab.App.Visualization;
using SortingLab.Core.Abstractions;
using SortingLab.Core.Algorithms.Text;
using SortingLab.Core.Models;
using SortingLab.Core.Text;

namespace SortingLab.App.ViewModels.Sections;

public sealed partial class TextSortViewModel : ObservableObject
{
    private readonly FileDialogService _dialogs;
    private readonly List<FrequencyRowViewModel> _allFrequencies = new();
    private readonly List<BenchmarkPoint> _benchmarkPoints = new();
    private string[] _words = Array.Empty<string>();
    private CancellationTokenSource? _sortCancellation;
    private CancellationTokenSource? _experimentCancellation;

    [ObservableProperty]
    private TextAlgorithmChoice? _selectedAlgorithm;

    [ObservableProperty]
    private string _textContent = string.Empty;

    [ObservableProperty]
    private string _textPreview = string.Empty;

    [ObservableProperty]
    private string _sourceName = "Текст не загружен";

    [ObservableProperty]
    private decimal _generatedWordCount = 500;

    [ObservableProperty]
    private decimal _seed = 2024;

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _sortMessage = "Загрузите текст или сгенерируйте его, затем выберите алгоритм.";

    [ObservableProperty]
    private string _sortElapsedText = "—";

    [ObservableProperty]
    private string _sortedPreview = string.Empty;

    [ObservableProperty]
    private string _wordSummary = "Слов: 0";

    [ObservableProperty]
    private string _searchSummary = "Найдено слов: 0";

    [ObservableProperty]
    private bool _isSorting;

    [ObservableProperty]
    private bool _hasSorted;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _experimentSizesText = "100, 500, 1000, 2000, 5000";

    [ObservableProperty]
    private decimal _experimentRepeats = 5;

    [ObservableProperty]
    private decimal _experimentSeed = 2024;

    [ObservableProperty]
    private string _experimentStatus = "Эксперименты ещё не запускались.";

    [ObservableProperty]
    private double _experimentProgress;

    [ObservableProperty]
    private bool _isExperimentRunning;

    [ObservableProperty]
    private bool _hasExperimentResults;

    [ObservableProperty]
    private int _benchmarkRevision;

    public TextSortViewModel(FileDialogService dialogs)
    {
        _dialogs = dialogs;

        var quick = new QuickSort();
        var radix = new RadixSort();
        Algorithms = new ObservableCollection<TextAlgorithmChoice>
        {
            new(quick, quick.Descriptor),
            new(radix, radix.Descriptor)
        };
        _selectedAlgorithm = Algorithms[0];

        Languages = new ObservableCollection<LanguageOption>
        {
            new(TextLanguage.English, "Английский"),
            new(TextLanguage.Russian, "Русский"),
            new(TextLanguage.Mixed, "Смешанный")
        };
        _selectedLanguage = Languages[0];

        GenerateText();
    }

    public ObservableCollection<TextAlgorithmChoice> Algorithms { get; }

    public ObservableCollection<LanguageOption> Languages { get; }

    public ObservableCollection<FrequencyRowViewModel> Frequencies { get; } = new();

    public ObservableCollection<FrequencyRowViewModel> TopWords { get; } = new();

    public ObservableCollection<BenchmarkRowViewModel> BenchmarkRows { get; } = new();

    public IReadOnlyList<BenchmarkPoint> BenchmarkPoints => _benchmarkPoints;

    public AlgorithmDescriptor? AlgorithmInfo => SelectedAlgorithm?.Descriptor;

    public bool HasText => _words.Length > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchText);

    public bool IsBusy => IsSorting || IsExperimentRunning;

    public bool CanEditText => !IsBusy;

    public string ExperimentInfo => "Медиана нескольких замеров; генерация и отрисовка не входят во время.";

    public string TotalWordsText => _words.Length.ToString("N0");

    public string UniqueWordsText => _allFrequencies.Count == 0 ? "—" : _allFrequencies.Count.ToString("N0");

    [RelayCommand]
    private async Task LoadTextAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            var path = await _dialogs.OpenFileAsync("Выберите текстовый файл", _dialogs.TextType);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var info = new FileInfo(path);
            if (info.Length > 16L * 1024 * 1024)
            {
                ErrorMessage = "Файл больше 16 МБ. Для экспериментов используйте генерацию текста.";
                return;
            }

            using var reader = new StreamReader(
                path,
                new UTF8Encoding(false, false),
                detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync();
            SetText(text, Path.GetFileName(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Не удалось прочитать файл: {exception.Message}";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Ошибка загрузки текста: {exception.Message}";
        }
    }

    [RelayCommand]
    private void GenerateText()
    {
        if (IsBusy)
        {
            return;
        }

        if (GeneratedWordCount < 1 || GeneratedWordCount > 50000)
        {
            ErrorMessage = "Количество слов должно быть от 1 до 50 000.";
            return;
        }

        var seed = (int)Math.Clamp(Seed, int.MinValue, int.MaxValue);
        var text = TextGenerator.Generate(
            (int)GeneratedWordCount,
            seed,
            SelectedLanguage?.Value ?? TextLanguage.English);
        SetText(text, $"Сгенерированный текст · зерно {seed}");
    }

    [RelayCommand]
    private void ClearText()
    {
        if (IsBusy)
        {
            return;
        }

        _words = Array.Empty<string>();
        TextContent = string.Empty;
        TextPreview = string.Empty;
        SourceName = "Текст не загружен";
        ResetResults();
        WordSummary = "Слов: 0";
        OnPropertyChanged(nameof(HasText));
        OnPropertyChanged(nameof(TotalWordsText));
        SortCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSort))]
    private async Task SortAsync()
    {
        if (SelectedAlgorithm is null || _words.Length == 0)
        {
            return;
        }

        IsSorting = true;
        ErrorMessage = null;
        SortMessage = "Сортировка выполняется…";
        _sortCancellation = new CancellationTokenSource();

        try
        {
            var algorithm = SelectedAlgorithm.Algorithm;
            var words = _words.ToArray();
            var token = _sortCancellation.Token;
            var (sorted, elapsed) = await Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = algorithm.Sort(words, token);
                stopwatch.Stop();
                return (result, stopwatch.Elapsed);
            }, token);

            var report = WordFrequencyAnalyzer.Analyze(sorted);
            _allFrequencies.Clear();
            _allFrequencies.AddRange(report.Frequencies.Select(item =>
                new FrequencyRowViewModel(
                    item.Word,
                    item.Count,
                    report.TotalWords == 0 ? 0 : item.Count * 100.0 / report.TotalWords)));
            TopWords.Clear();
            foreach (var item in report.TopWords)
            {
                TopWords.Add(new FrequencyRowViewModel(
                    item.Word,
                    item.Count,
                    report.TotalWords == 0 ? 0 : item.Count * 100.0 / report.TotalWords));
            }

            ApplySearch();
            HasSorted = true;
            SortElapsedText = elapsed.TotalMilliseconds < 1
                ? $"{elapsed.TotalMilliseconds * 1000:0} мкс"
                : $"{elapsed.TotalMilliseconds:0.000} мс";
            SortedPreview = sorted.Length == 0
                ? string.Empty
                : string.Join("  ", sorted.Take(160)) + (sorted.Length > 160 ? " …" : string.Empty);
            SortMessage = $"Отсортировано слов: {sorted.Length:N0} · уникальных: {report.UniqueWords:N0}.";
            WordSummary = $"Слов: {report.TotalWords:N0} · уникальных: {report.UniqueWords:N0}";
            OnPropertyChanged(nameof(TotalWordsText));
            OnPropertyChanged(nameof(UniqueWordsText));
        }
        catch (OperationCanceledException)
        {
            SortMessage = "Сортировка отменена.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Ошибка сортировки: {exception.Message}";
        }
        finally
        {
            IsSorting = false;
            _sortCancellation?.Dispose();
            _sortCancellation = null;
        }
    }

    [RelayCommand]
    private void CancelSort()
    {
        _sortCancellation?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanRunExperiments))]
    private async Task RunExperimentsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var sizes = ParseSizes(ExperimentSizesText);
        if (sizes.Count == 0)
        {
            ErrorMessage = "Укажите размеры текстов через запятую или пробел.";
            return;
        }

        if (sizes.Any(size => size > 50000))
        {
            ErrorMessage = "Максимальный размер в эксперименте — 50 000 слов.";
            return;
        }

        var repeats = (int)Math.Clamp(ExperimentRepeats, 1, 20);
        var seed = (int)Math.Clamp(ExperimentSeed, int.MinValue, int.MaxValue);
        IsExperimentRunning = true;
        ErrorMessage = null;
        HasExperimentResults = false;
        BenchmarkRows.Clear();
        _benchmarkPoints.Clear();
        BenchmarkRevision++;
        ExperimentProgress = 0;
        ExperimentStatus = "Подготовка…";
        _experimentCancellation = new CancellationTokenSource();
        var token = _experimentCancellation.Token;

        try
        {
            var progress = new Progress<BenchmarkProgress>(value =>
            {
                ExperimentProgress = value.TotalSizes == 0
                    ? 0
                    : value.CompletedSizes * 100.0 / value.TotalSizes;
                ExperimentStatus = $"{value.Stage} · {value.WordCount:N0} слов · " +
                                   $"{value.CompletedSizes}/{value.TotalSizes}";
            });

            var runner = new BenchmarkRunner();
            var report = await Task.Run(
                () => runner.RunAsync(
                    new BenchmarkOptions(sizes, repeats, WarmupRuns: 1, Seed: seed, Language: TextLanguage.English),
                    progress,
                    token),
                token);

            foreach (var row in report.Rows)
            {
                BenchmarkRows.Add(new BenchmarkRowViewModel(
                    row.WordCount,
                    row.QuickSortMedianMs,
                    row.RadixSortMedianMs,
                    row.QuickSortMinMs,
                    row.RadixSortMinMs));
                _benchmarkPoints.Add(new BenchmarkPoint(row.WordCount, row.QuickSortMedianMs, row.RadixSortMedianMs));
            }

            HasExperimentResults = true;
            BenchmarkRevision++;
            ExperimentProgress = 100;
            ExperimentStatus = $"Готово. Размеров: {report.Rows.Count}; медиана из {report.Repeats} замеров.";
        }
        catch (OperationCanceledException)
        {
            ExperimentStatus = "Эксперименты отменены.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Ошибка экспериментов: {exception.Message}";
            ExperimentStatus = "Эксперименты не завершены.";
        }
        finally
        {
            IsExperimentRunning = false;
            _experimentCancellation?.Dispose();
            _experimentCancellation = null;
        }
    }

    [RelayCommand]
    private void CancelExperiments()
    {
        _experimentCancellation?.Cancel();
    }

    public void Shutdown()
    {
        _sortCancellation?.Cancel();
        _experimentCancellation?.Cancel();
    }

    private void SetText(string text, string sourceName)
    {
        TextContent = text;
        TextPreview = text.Length <= 3500 ? text : text[..3500] + "\n…";
        SourceName = sourceName;
        _words = TextTokenizer.Tokenize(text).ToArray();
        ResetResults();
        WordSummary = $"Слов: {_words.Length:N0}";
        OnPropertyChanged(nameof(HasText));
        OnPropertyChanged(nameof(TotalWordsText));
        SortCommand.NotifyCanExecuteChanged();
        ErrorMessage = null;
        SortMessage = _words.Length == 0
            ? "В тексте не найдено слов."
            : "Текст готов. Выберите алгоритм и нажмите «Сортировать».";
    }

    private void ResetResults()
    {
        _allFrequencies.Clear();
        Frequencies.Clear();
        TopWords.Clear();
        SortedPreview = string.Empty;
        SortElapsedText = "—";
        HasSorted = false;
        SearchText = string.Empty;
        SearchSummary = "Найдено слов: 0";
        OnPropertyChanged(nameof(UniqueWordsText));
    }

    private void ApplySearch()
    {
        var query = SearchText.Trim();
        var filtered = query.Length == 0
            ? _allFrequencies
            : _allFrequencies
                .Where(item => item.Word.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Frequencies.Clear();
        foreach (var item in filtered)
        {
            Frequencies.Add(item);
        }

        SearchSummary = $"Найдено слов: {filtered.Count:N0}";
        OnPropertyChanged(nameof(HasSearch));
        OnPropertyChanged(nameof(UniqueWordsText));
    }

    private static IReadOnlyList<int> ParseSizes(string text)
    {
        var separators = new[] { ',', ';', ' ', '\r', '\n', '\t' };
        var sizes = text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var value) ? value : -1)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        return sizes;
    }

    private bool CanSort() => !IsBusy && _words.Length > 0 && SelectedAlgorithm is not null;

    private bool CanRunExperiments() => !IsBusy;

    private void NotifyBusy()
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanEditText));
        SortCommand.NotifyCanExecuteChanged();
        RunExperimentsCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSortingChanged(bool value) => NotifyBusy();

    partial void OnIsExperimentRunningChanged(bool value) => NotifyBusy();

    partial void OnSearchTextChanged(string value)
    {
        if (HasSorted)
        {
            ApplySearch();
        }
    }

    partial void OnErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasError));

    partial void OnSelectedAlgorithmChanged(TextAlgorithmChoice? value)
    {
        OnPropertyChanged(nameof(AlgorithmInfo));
        SortCommand.NotifyCanExecuteChanged();
        if (HasSorted)
        {
            ResetResults();
            SortMessage = "Алгоритм изменён. Выполните сортировку заново.";
        }
    }

    partial void OnHasExperimentResultsChanged(bool value) =>
        OnPropertyChanged(nameof(HasExperimentResults));

    partial void OnHasSortedChanged(bool value)
    {
        OnPropertyChanged(nameof(HasSorted));
        OnPropertyChanged(nameof(TotalWordsText));
    }
}
