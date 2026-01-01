using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using DebugAgentPrototype.Services.tools;
using ReactiveUI;
using ToolCall = DebugAgentPrototype.Services.tools.ToolCall;

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

    public MainViewModel(
        AgentService agentService,
        AppState appState,
        LldbService lldbService,
        LldbOutputViewModel lldbOutputViewModel)
    {
        _agentService = agentService;
        _appState = appState;
        _lldbService = lldbService;
        LldbOutputViewModel = lldbOutputViewModel;

        _appState.Messages = AgentService.InitMessages();

        UIMessages = new ObservableCollection<UIMessage>();

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
            if (COMPRESS_TOOL_CALL_SEQUENCE && message.Role == MessageRole.Tool) {
                var lastUIMessage = UIMessages.LastOrDefault();
                if (lastUIMessage != null && lastUIMessage.Role == MessageRole.Tool) {
                    var toolCallUIMessage = new UIMessage(message);
                    UIMessages.Remove(lastUIMessage);
                    UIMessages.Add(toolCallUIMessage);
                }
            } else {
                UIMessages.Add(new UIMessage(message));
            }
        });
    }

    private List<UIMessage> ToViewModelMessagesNew(List<Message> messages)
    {
        var viewModelMessages = messages
            .Where(message => message.Role != MessageRole.System && message.Role != MessageRole.Tool)
            .Select(message => new UIMessage(message))
            .ToList();

        var lastTextMessageIndex = messages.FindLastIndex(m => (m is UserMessage) || m is AssistantMessage && !string.IsNullOrEmpty((m as AssistantMessage)?.Text));
        var lastAssistantToolCallMessageIndex = messages.FindLastIndex(m => m.Role == MessageRole.Assistant && m is AssistantMessage am && am.ToolCallRequests.Count > 0);

        if (lastAssistantToolCallMessageIndex > lastTextMessageIndex && messages[lastAssistantToolCallMessageIndex] is AssistantMessage assistantMessage)
        {
            var processingStatus = "Processing tool calls:" + string.Join(", ", assistantMessage.ToolCallRequests.Select(tcr => tcr.Name));
            viewModelMessages.Add(new UIMessage(messages[lastAssistantToolCallMessageIndex]) { ProcessingStatus = processingStatus });
        }
        
        return viewModelMessages;
    }

    private List<UIMessage> ToViewModelMessages(List<Message> messages)
    {
        return messages
            .Where(message => message.Role != MessageRole.System)
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

public class UIMessage {

    public MessageRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<ToolCallRequest> ToolCallRequests { get; set; } = new List<ToolCallRequest>();
    public List<UIToolCall> ToolCalls { get; set; } = new List<UIToolCall>();
    public string ProcessingStatus { get; set; } = string.Empty;

    public UIMessage(Message message)
    {
        Role = message.Role;
        
        if (message is UserMessage userMessage)
        {
            Text = userMessage.Text;
        }
        if (message is SystemMessage systemMessage)
        {
            Text = systemMessage.Text;
        }
        if (message is AssistantMessage assistantMessage)
        {
            Text = assistantMessage.Text ?? string.Empty;
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

    public UIToolCall(ToolCall toolCall)
    {
        Id = toolCall.Id;
        Name = toolCall.Name;
        Arguments = toolCall.Arguments;
        Result = toolCall.Result != null ? JsonSerializer.Serialize(toolCall.Result) : string.Empty;
    }
}

