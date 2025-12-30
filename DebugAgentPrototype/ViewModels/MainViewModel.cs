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
    private bool _isBusy;

    private bool COMPRESS_TOOL_CALL_SEQUENCE = false;

    public ObservableCollection<UIMessage> UIMessages { get; }
    public LldbOutputViewModel LldbOutputViewModel { get; }

    public MainViewModel()
    {
        _appState = new AppState();
        _lldbService = new LldbService(_appState);
        var openRouterService = new OpenRouterService();
        var toolsService = new ToolsService(_appState, _lldbService);
        _agentService = new AgentService(_lldbService, openRouterService, _appState, toolsService);

        _appState.Messages = _agentService.InitMessages();

        UIMessages = new ObservableCollection<UIMessage>();
        LldbOutputViewModel = new LldbOutputViewModel(_lldbService, _appState);

        _agentService.MessageAdded += OnMessageAdded;

        // SendMessageCommand is enabled when not busy and user input is not empty
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
            await _lldbService.InitializeAsync(CancellationToken.None);
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

    private void OnMessageAdded(object? sender, ChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (COMPRESS_TOOL_CALL_SEQUENCE && message.Role == ChatMessageRole.Tool) {
                var lastUIMessage = UIMessages.LastOrDefault();
                if (lastUIMessage != null && lastUIMessage.Role == ChatMessageRole.Tool) {
                    var toolCallUIMessage = new UIMessage(message);
                    UIMessages.Remove(lastUIMessage);
                    UIMessages.Add(toolCallUIMessage);
                }
            } else {
                UIMessages.Add(new UIMessage(message));
            }
        });
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
            _agentService.addUserMessage(userText);

            Dispatcher.UIThread.Post(() =>
            {
                LldbOutputViewModel.UpdateRunningState();
            });
            await _agentService.ProcessLastUserMessageAsync(CancellationToken.None);
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

