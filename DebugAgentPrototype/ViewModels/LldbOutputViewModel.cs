using System;
using System.Reactive;
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
    private readonly AgentService _agentService;
    private string _lldbInput = string.Empty;

    public LldbOutputViewModel(LldbService lldbService, AppState appState, AgentService agentService)
    {
        _lldbService = lldbService;
        _appState = appState;
        _agentService = agentService;
        _lldbService.OutputReceived += OnLldbOutputReceived;

        var canSendLldb = this.WhenAnyValue(
            x => x.LldbInput,
            input => !string.IsNullOrWhiteSpace(input));

        SendLldbCommand = ReactiveCommand.CreateFromTask(
            SendLldbCommandAsync,
            canSendLldb);
    }

    public string LldbOutput => _appState.LldbOutput;

    public string LldbInput
    {
        get => _lldbInput;
        set => this.RaiseAndSetIfChanged(ref _lldbInput, value);
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
        if (string.IsNullOrWhiteSpace(LldbInput))
            return;

        var command = LldbInput.Trim();
        LldbInput = string.Empty;

        try
        {
            await _agentService.SendUserLldbCommandAsync(command);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _appState.LldbOutput += $"Error: {ex.Message}\n";
                this.RaisePropertyChanged(nameof(LldbOutput));
            });
        }
    }
}

