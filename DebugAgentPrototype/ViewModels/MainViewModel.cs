using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using ReactiveUI;

namespace DebugAgentPrototype.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly AgentService _agentService;
    private readonly AppState _appState;
    private readonly LldbService _lldbService;
    private string _userInput = string.Empty;
    private string _lldbInput = string.Empty;
    private string _lldbOutput = string.Empty;
    private bool _isBusy;
    private bool _isLldbRunning;

    public ObservableCollection<UIMessage> Messages { get; }

    public MainViewModel()
    {
        _appState = new AppState();
        _lldbService = new LldbService();
        var openRouterService = new OpenRouterService();
        _agentService = new AgentService(_lldbService, openRouterService);

        _appState.Messages = _agentService.InitMessages(); //TODO init in a proper place

        Messages = new ObservableCollection<UIMessage>();

        // Subscribe to lldb output
        _lldbService.OutputReceived += OnLldbOutputReceived;
        _agentService.MessagesUpdated += OnMessagesUpdated;

        // SendMessageCommand is enabled when not busy and user input is not empty
        var canSend = this.WhenAnyValue(
            x => x.IsBusy,
            x => x.UserInput,
            (busy, input) => !busy && !string.IsNullOrWhiteSpace(input));

        SendMessageCommand = ReactiveCommand.CreateFromTask(
            SendMessageAsync,
            canSend);

        // SendLldbCommand is enabled when lldb is running and input is not empty
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
        get => _lldbOutput;
        set => this.RaiseAndSetIfChanged(ref _lldbOutput, value);
    }

    public string LldbInput
    {
        get => _lldbInput;
        set => this.RaiseAndSetIfChanged(ref _lldbInput, value);
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

    public bool IsLldbRunning
    {
        get => _isLldbRunning;
        set => this.RaiseAndSetIfChanged(ref _isLldbRunning, value);
    }

    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<Unit, Unit> SendLldbCommand { get; }

    private void OnLldbOutputReceived(object? sender, string output)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LldbOutput += output + "\n";
        });
    }

    private void OnMessagesUpdated(object? sender, List<ChatMessage> messages)
    {   //TODO optimize 
        Dispatcher.UIThread.Post(() =>
        {
            Messages.Clear();
            foreach (var message in ToViewModelMessages(messages))
            {
                Messages.Add(message);
            }
        });
    }

    private async Task SendLldbCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(LldbInput) || !_lldbService.IsRunning)
            return;

        var command = LldbInput.Trim();
        LldbInput = string.Empty; // Clear input immediately

        try
        {
            await _lldbService.SendCommandAsync(command, CancellationToken.None);
            // Update running state in case it changed
            Dispatcher.UIThread.Post(() =>
            {
                IsLldbRunning = _lldbService.IsRunning;
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                LldbOutput += $"Error: {ex.Message}\n";
                IsLldbRunning = _lldbService.IsRunning;
            });
        }
    }


    private List<UIMessage> ToViewModelMessagesNew(List<ChatMessage> messages)
    {
        var viewModelMessages = messages
            .Where(message => message.Role != ChatMessageRole.System && message.Role != ChatMessageRole.Tool)
            .Select(message => new UIMessage(message))
            .ToList();

        var lastTextMessageIndex = messages.FindLastIndex(m => !string.IsNullOrEmpty(m.Text));
        var lastAssistantToolCallMessageIndex = messages.FindLastIndex(m => m.Role == ChatMessageRole.Assistant && m is AssistantMessage am && am.ToolCallRequests.Count > 0);

        if (lastAssistantToolCallMessageIndex > lastTextMessageIndex && messages[lastAssistantToolCallMessageIndex] is AssistantMessage assistantMessage)
        {
            var processingStatus = "Processing tool calls:" + string.Join(", ", assistantMessage.ToolCallRequests.Select(tcr => tcr.Name));
            viewModelMessages.Add(new UIMessage(messages[lastAssistantToolCallMessageIndex]) { ProcessingStatus = processingStatus });
        }
        
        return viewModelMessages;
    }

    private List<UIMessage> ToViewModelMessages(List<ChatMessage> messages)
    {
        return messages
            .Where(message => message.Role != ChatMessageRole.System)
            .Select(message => new UIMessage(message))
            .ToList();
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
            _agentService.addUserMessage(userText, _appState);

            Dispatcher.UIThread.Post(() =>
            {
                IsLldbRunning = _lldbService.IsRunning;
            });
            await _agentService.ProcessLastUserMessageAsync(_appState, CancellationToken.None);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class UIMessage {

    public ChatMessageRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<ToolCallRequest> ToolCallRequests { get; set; } = new List<ToolCallRequest>();
    public List<UIToolCall> ToolCalls { get; set; } = new List<UIToolCall>();
    public string ProcessingStatus { get; set; } = string.Empty;

    public UIMessage(ChatMessage message)
    {
        Role = message.Role;
        Text = message.Text;
        if (message is AssistantMessage assistantMessage)
        {
            ToolCallRequests = assistantMessage.ToolCallRequests;
        }
        if (message is ToolCallMessage toolCallMessage)
        {
            ToolCalls = toolCallMessage.ToolCalls.Select(tc => new UIToolCall(tc)).ToList();
        }
    }
}

public class UIToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    public UIToolCall(Services.ToolCall toolCall)
    {
        Id = toolCall.Id;
        Name = toolCall.Name;
        Arguments = toolCall.Arguments;
        Result = toolCall.Result != null ? JsonSerializer.Serialize(toolCall.Result) : string.Empty;
    }
}

