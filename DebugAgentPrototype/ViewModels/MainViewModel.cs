using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using ReactiveUI;

namespace DebugAgentPrototype.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly AgentService _agentService;
    private readonly LldbService _lldbService;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private readonly bool _compressToolCallSequence = false;

    public ObservableCollection<UiMessage> UiMessages { get; }
    public LldbOutputViewModel LldbOutputViewModel { get; }

    public MainViewModel(
        AgentService agentService,
        AppState appState,
        LldbService lldbService,
        LldbOutputViewModel lldbOutputViewModel)
    {
        _agentService = agentService;
        _lldbService = lldbService;
        LldbOutputViewModel = lldbOutputViewModel;

        appState.Messages = AgentService.InitMessages();

        UiMessages = [];

        _agentService.MessageAdded += OnMessageAdded;

        var canSend = this.WhenAnyValue(
            x => x.IsBusy,
            x => x.UserInput,
            (busy, input) => !busy && !string.IsNullOrWhiteSpace(input));

        SendMessageCommand = ReactiveCommand.CreateFromTask(
            SendMessageAsync,
            canSend);
    }

    public async Task InitializeLldbAsync()
    {
        try
        {
            await _lldbService.InitializeAsync();
            Dispatcher.UIThread.Post(() =>
            {
                LldbOutputViewModel.UpdateRunningState();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] Error initializing LLDB: {ex.Message}");
        }
    }

    public string UserInput
    {
        get => _userInput;
        set => this.RaiseAndSetIfChanged(ref _userInput, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }

    private void OnMessageAdded(object? sender, Message message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (message is UserLldbCommandMessage) return;
            
            if (_compressToolCallSequence && message.Role == MessageRole.Tool) {
                var lastUiMessage = UiMessages.LastOrDefault();
                if (lastUiMessage is { Role: MessageRole.Tool }) {
                    var toolCallUiMessage = new UiMessage(message);
                    UiMessages.Remove(lastUiMessage);
                    UiMessages.Add(toolCallUiMessage);
                }
            } else {
                UiMessages.Add(new UiMessage(message));
            }
        });
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userText = UserInput.Trim();
        UserInput = string.Empty; 
        IsBusy = true;

        try
        {
            _agentService.AddUserMessage(userText);

            Dispatcher.UIThread.Post(() =>
            {
                LldbOutputViewModel.UpdateRunningState();
            });
            await _agentService.ProcessLastUserMessageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class UiMessage {

    public MessageRole Role { get; }
    public string? Text { get; set; } = string.Empty;
    public List<ToolCallRequest> ToolCallRequests { get; set; } = new();
    public List<UiToolCall> ToolCalls { get; set; } = new();

    public UiMessage(Message message)
    {
        Role = message.Role;
        
        switch (message)
        {
            case UserMessage userMessage:
                Text = userMessage.Text;
                break;
            case SystemMessage systemMessage:
                Text = systemMessage.Text;
                break;
            case AssistantMessage assistantMessage:
                Text = assistantMessage.Text ?? string.Empty;
                ToolCallRequests = assistantMessage.ToolCallRequests;
                break;
            case ToolCallMessage toolCallMessage:
                ToolCalls = [new UiToolCall(toolCallMessage.ToolCall)];
                break;
        }
    }
}

public class UiToolCall(ToolCall toolCall)
{
    public string Id { get; set; } = toolCall.Request.Id;
    public string Name { get; set; } = toolCall.Request.Name;
    public string Arguments { get; set; } = toolCall.Request.Arguments;
    public string Result { get; set; } = toolCall.Result != null ? JsonSerializer.Serialize(toolCall.Result) : string.Empty;
}

