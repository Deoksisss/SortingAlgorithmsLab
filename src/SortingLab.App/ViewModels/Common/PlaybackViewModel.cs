using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Services;
using SortingLab.Core.Models;

namespace SortingLab.App.ViewModels.Common;

/// <summary>
/// Общий проигрыватель шагов: пауза, продолжение, ручной шаг, остановка,
/// сброс и регулировка задержки. Работает с любым DemoStep.
/// </summary>
public sealed partial class PlaybackViewModel : ObservableObject
{
    private const int MaxLogEntries = 20000;
    private Func<CancellationToken, DemonstrationStream>? _streamFactory;
    private Action<DemoStep>? _applyStep;
    private Action? _resetVisuals;
    private Action? _beforeStart;
    private Func<string?>? _validateStart;
    private CancellationTokenSource? _cts;
    private Task? _currentRun;
    private TaskCompletionSource<bool>? _advanceSignal;
    private bool _paused;
    private bool _singleStep;
    private bool _stopRequested;

    [ObservableProperty]
    private DemoPlaybackState _state = DemoPlaybackState.Idle;

    [ObservableProperty]
    private double _stepDelayMilliseconds = 80;

    [ObservableProperty]
    private int _stepCount;

    [ObservableProperty]
    private string _currentTitle = "Демонстрация не запущена";

    [ObservableProperty]
    private string _currentKind = "—";

    [ObservableProperty]
    private string _currentExplanation = "Нажмите «Запустить», чтобы начать пошаговую демонстрацию.";

    [ObservableProperty]
    private OperationStatistics _statistics = OperationStatistics.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLogTruncated;

    public ObservableCollection<LogEntryViewModel> Log { get; } = new();

    public bool IsRunning => State == DemoPlaybackState.Running;

    public bool IsPaused => State == DemoPlaybackState.Paused;

    public bool IsCompleted => State == DemoPlaybackState.Completed;

    public bool IsCancelled => State == DemoPlaybackState.Cancelled;

    public bool IsFaulted => State == DemoPlaybackState.Faulted;

    public bool IsIdle => State == DemoPlaybackState.Idle;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasLog => Log.Count > 0;

    public bool IsActive => IsRunning || IsPaused;

    public string StateText => State switch
    {
        DemoPlaybackState.Idle => "Готово к запуску",
        DemoPlaybackState.Running => "Выполняется",
        DemoPlaybackState.Paused => "Пауза",
        DemoPlaybackState.Completed => "Завершено",
        DemoPlaybackState.Cancelled => "Отменено",
        DemoPlaybackState.Faulted => "Ошибка",
        _ => string.Empty
    };

    public string DelayText => $"{StepDelayMilliseconds:0} мс";

    public string StepText => StepCount == 0 ? "Шагов: 0" : $"Шаг {StepCount}";

    public string LogCountText => Log.Count == 0
        ? "Журнал пуст"
        : $"Записей: {Log.Count}" + (IsLogTruncated ? " (показаны последние)" : string.Empty);

    public void Configure(
        Func<CancellationToken, DemonstrationStream> streamFactory,
        Action<DemoStep> applyStep,
        Action resetVisuals,
        Action? beforeStart = null,
        Func<string?>? validateStart = null)
    {
        _streamFactory = streamFactory;
        _applyStep = applyStep;
        _resetVisuals = resetVisuals;
        _beforeStart = beforeStart;
        _validateStart = validateStart;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (!BeginSession(paused: false, singleStep: false))
        {
            return;
        }

        if (_currentRun is not null)
        {
            await _currentRun;
        }
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        if (State != DemoPlaybackState.Running)
        {
            return;
        }

        _paused = true;
        SetState(DemoPlaybackState.Paused);
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void Resume()
    {
        if (State != DemoPlaybackState.Paused)
        {
            return;
        }

        _paused = false;
        _singleStep = false;
        SetState(DemoPlaybackState.Running);
        ReleaseAdvance();
    }

    [RelayCommand(CanExecute = nameof(CanStep))]
    private void Step()
    {
        if (State is DemoPlaybackState.Idle or DemoPlaybackState.Completed
            or DemoPlaybackState.Cancelled or DemoPlaybackState.Faulted)
        {
            BeginSession(paused: true, singleStep: true);
            return;
        }

        if (State == DemoPlaybackState.Running)
        {
            _paused = true;
            SetState(DemoPlaybackState.Paused);
        }

        _singleStep = true;
        ReleaseAdvance();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        if (!IsActive)
        {
            return;
        }

        _stopRequested = true;
        _cts?.Cancel();
        ReleaseAdvance();
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private async Task ResetAsync()
    {
        _stopRequested = true;
        _cts?.Cancel();
        ReleaseAdvance();

        if (_currentRun is not null)
        {
            try
            {
                await _currentRun;
            }
            catch (OperationCanceledException)
            {
                // Ожидаемая отмена.
            }
        }

        _cts?.Dispose();
        _cts = null;
        _currentRun = null;
        _paused = false;
        _singleStep = false;
        _stopRequested = false;
        Log.Clear();
        IsLogTruncated = false;
        StepCount = 0;
        Statistics = OperationStatistics.Empty;
        ErrorMessage = null;
        CurrentTitle = "Демонстрация не запущена";
        CurrentKind = "—";
        CurrentExplanation = "Нажмите «Запустить», чтобы начать пошаговую демонстрацию.";
        _resetVisuals?.Invoke();
        SetState(DemoPlaybackState.Idle);
        OnPropertyChanged(nameof(HasLog));
        OnPropertyChanged(nameof(LogCountText));
        OnPropertyChanged(nameof(HasError));
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        SetState(DemoPlaybackState.Faulted);
    }

    public void Shutdown()
    {
        _stopRequested = true;
        _cts?.Cancel();
        ReleaseAdvance();
    }

    private bool BeginSession(bool paused, bool singleStep)
    {
        if (State is DemoPlaybackState.Running or DemoPlaybackState.Paused)
        {
            return false;
        }

        var validationError = _validateStart?.Invoke();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            SetError(validationError);
            return false;
        }

        if (_streamFactory is null || _applyStep is null)
        {
            SetError("Внутренняя ошибка: обработчик демонстрации не настроен.");
            return false;
        }

        ErrorMessage = null;
        _stopRequested = false;
        _paused = paused;
        _singleStep = singleStep;

        _beforeStart?.Invoke();
        Log.Clear();
        IsLogTruncated = false;
        StepCount = 0;
        Statistics = OperationStatistics.Empty;
        CurrentTitle = "Подготовка";
        CurrentKind = "—";
        CurrentExplanation = "Алгоритм готовится к выполнению.";
        OnPropertyChanged(nameof(HasLog));
        OnPropertyChanged(nameof(LogCountText));

        _cts = new CancellationTokenSource();
        DemonstrationStream stream;
        try
        {
            stream = _streamFactory(_cts.Token);
        }
        catch (Exception exception)
        {
            SetError($"Не удалось запустить демонстрацию: {exception.Message}");
            _cts.Dispose();
            _cts = null;
            _paused = false;
            _singleStep = false;
            return false;
        }

        SetState(paused ? DemoPlaybackState.Paused : DemoPlaybackState.Running);
        _currentRun = RunLoopAsync(stream, _cts.Token);
        return true;
    }

    private async Task RunLoopAsync(DemonstrationStream stream, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var step in stream.Steps.WithCancellation(cancellationToken))
            {
                if (_paused && !step.IsInitial)
                {
                    if (!_singleStep)
                    {
                        await WaitForAdvanceAsync(cancellationToken);
                    }

                    _singleStep = false;
                }

                Apply(step);

                if (!_paused)
                {
                    var delay = (int)Math.Clamp(StepDelayMilliseconds, 0, 5000);
                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
            }

            await stream.Completion;
            SetState(_stopRequested ? DemoPlaybackState.Cancelled : DemoPlaybackState.Completed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(DemoPlaybackState.Cancelled);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            SetState(DemoPlaybackState.Faulted);
        }
        finally
        {
            ReleaseAdvance();
            if (_stopRequested && State is DemoPlaybackState.Running or DemoPlaybackState.Paused)
            {
                SetState(DemoPlaybackState.Cancelled);
            }

            _cts?.Dispose();
            _cts = null;
            _currentRun = null;
        }
    }

    private void Apply(DemoStep step)
    {
        StepCount = step.Number;
        CurrentTitle = step.Title;
        CurrentKind = step.KindLabel;
        CurrentExplanation = step.Explanation;
        Statistics = step.Statistics;

        Log.Add(new LogEntryViewModel(
            step.Number,
            step.KindLabel,
            step.Details,
            step.Explanation,
            step.Title));

        if (Log.Count > MaxLogEntries)
        {
            Log.RemoveAt(0);
            IsLogTruncated = true;
        }

        OnPropertyChanged(nameof(HasLog));
        OnPropertyChanged(nameof(LogCountText));
        _applyStep?.Invoke(step);
    }

    private async Task WaitForAdvanceAsync(CancellationToken cancellationToken)
    {
        while (_paused && !_singleStep)
        {
            var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _advanceSignal = signal;
            using var registration = cancellationToken.Register(() => signal.TrySetCanceled(cancellationToken));
            await signal.Task;
            if (ReferenceEquals(_advanceSignal, signal))
            {
                _advanceSignal = null;
            }
        }
    }

    private void ReleaseAdvance()
    {
        var signal = _advanceSignal;
        _advanceSignal = null;
        signal?.TrySetResult(true);
    }

    private void SetState(DemoPlaybackState state)
    {
        State = state;
    }

    private bool CanStart() =>
        State is DemoPlaybackState.Idle or DemoPlaybackState.Completed
            or DemoPlaybackState.Cancelled or DemoPlaybackState.Faulted;

    private bool CanPause() => State == DemoPlaybackState.Running;

    private bool CanResume() => State == DemoPlaybackState.Paused;

    private bool CanStep() =>
        State is DemoPlaybackState.Idle or DemoPlaybackState.Running or DemoPlaybackState.Paused;

    private bool CanStop() => State is DemoPlaybackState.Running or DemoPlaybackState.Paused;

    private bool CanReset() => State != DemoPlaybackState.Idle;

    partial void OnStateChanged(DemoPlaybackState value)
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsCancelled));
        OnPropertyChanged(nameof(IsFaulted));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(StateText));
        StartCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        StepCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
    }

    partial void OnStepDelayMillisecondsChanged(double value) =>
        OnPropertyChanged(nameof(DelayText));

    partial void OnStepCountChanged(int value) =>
        OnPropertyChanged(nameof(StepText));

    partial void OnErrorMessageChanged(string? value) =>
        OnPropertyChanged(nameof(HasError));
}
