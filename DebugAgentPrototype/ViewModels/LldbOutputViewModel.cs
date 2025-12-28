using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using ReactiveUI;

namespace DebugAgentPrototype.ViewModels;

public class LldbOutputViewModel : ReactiveObject
{
    private readonly LldbService _lldbService;
    private readonly AppState _appState;
    private string _lldbInput = string.Empty;
    private bool _isLldbRunning;

    public LldbOutputViewModel(LldbService lldbService, AppState appState)
    {
        _lldbService = lldbService;
        _appState = appState;
        _lldbService.OutputReceived += OnLldbOutputReceived;

        var canSendLldb = this.WhenAnyValue(
            x => x.IsLldbRunning,
            x => x.LldbInput,
            (running, input) => running && !string.IsNullOrWhiteSpace(input));

        SendLldbCommand = ReactiveCommand.CreateFromTask(
            SendLldbCommandAsync,
            canSendLldb);
    }

    public string LldbOutput
    {
        get => _appState.LldbOutput;
    }

    public string LldbInput
    {
        get => _lldbInput;
        set => this.RaiseAndSetIfChanged(ref _lldbInput, value);
    }

    public bool IsLldbRunning
    {
        get => _isLldbRunning;
        set => this.RaiseAndSetIfChanged(ref _isLldbRunning, value);
    }

    public ReactiveCommand<Unit, Unit> SendLldbCommand { get; }

    private void OnLldbOutputReceived(object? sender, string output)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.RaisePropertyChanged(nameof(LldbOutput));
        });
    }

    private async Task SendLldbCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(LldbInput) || !_lldbService.IsRunning)
            return;

        var command = LldbInput.Trim();
        LldbInput = string.Empty;

        try
        {
            await _lldbService.SendCommandAsync(command, CancellationToken.None);
            Dispatcher.UIThread.Post(() =>
            {
                IsLldbRunning = _lldbService.IsRunning;
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _appState.LldbOutput += $"Error: {ex.Message}\n";
                this.RaisePropertyChanged(nameof(LldbOutput));
                IsLldbRunning = _lldbService.IsRunning;
            });
        }
    }

    public void UpdateRunningState()
    {
        IsLldbRunning = _lldbService.IsRunning;
    }
}

