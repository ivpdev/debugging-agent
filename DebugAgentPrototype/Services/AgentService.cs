using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class AgentService
{
    private const int MaxTurns = 10;
    
    private readonly LldbService _lldbService;
    private readonly OpenRouterService _openRouterService;
    private readonly AppState _appState;
    private readonly ToolsService _toolsService;

    public event EventHandler<List<ChatMessage>>? MessagesUpdated;

    public AgentService(LldbService lldbService, OpenRouterService llmService, AppState appState, ToolsService toolsService)
    {
        _lldbService = lldbService;
        _openRouterService = llmService;
        _appState = appState;
        _toolsService = toolsService;
    }

    private bool IsTaskComplete(AssistantMessage assistantMessage)
    {
        return assistantMessage.ToolCallRequests.Count == 0 && !string.IsNullOrEmpty(assistantMessage.Text);
        
    }

    private bool IsMaxTurnsReached(List<ChatMessage> messages)
    {
        var lastUserMessageIndex = messages.FindLastIndex(m => m.Role == ChatMessageRole.User);
    
        var assistantMessagesAfterLastUserCount = messages
            .Skip(lastUserMessageIndex + 1)
            .Where(m => m.Role == ChatMessageRole.Assistant).Count();
        
        return assistantMessagesAfterLastUserCount > MaxTurns;
    }

    public void addUserMessage(string userText)
    {
        _appState.Messages.Add(new ChatMessage { Role = ChatMessageRole.User, Text = userText });
        MessagesUpdated?.Invoke(this, _appState.Messages);
    }

    public async Task ProcessLastUserMessageAsync(CancellationToken ct) {
        var tools = ToolsService.GetTools();
        
        ILlmResponse response;
        AssistantMessage assistantMessage;

        do {
            response = await _openRouterService.CallModelAsync(_appState.Messages, tools);

            assistantMessage = toAssistantMessage(response);
            _appState.Messages.Add(assistantMessage);
            MessagesUpdated?.Invoke(this, _appState.Messages);

            if (assistantMessage.ToolCallRequests.Count > 0) {
                var toolCalls = await _toolsService.callToolsAsync(assistantMessage.ToolCallRequests, ct);
                _appState.Messages.Add(new ToolCallMessage { ToolCalls = toolCalls });
                MessagesUpdated?.Invoke(this, _appState.Messages);
            }
        } while (!IsTaskComplete(assistantMessage) && !IsMaxTurnsReached(_appState.Messages));
    
    }

    private void PrintMessageHistory(List<ChatMessage> messages)
    {
        Console.WriteLine("=== Full Message History ===");
        foreach (var msg in messages)
        {
            Console.WriteLine($"[{msg.Role}] {msg.Text}");
            if (msg is AssistantMessage am && am.ToolCallRequests.Count > 0)
            {
                foreach (var toolCall in am.ToolCallRequests)
                {
                    Console.WriteLine($"  Tool Call: {toolCall.Name}({toolCall.Arguments})");
                }
            }
            if (msg is ToolCallMessage tm && tm.ToolCalls.Count > 0)
            {
                foreach (var toolCall in tm.ToolCalls)
                {
                    Console.WriteLine($"  Tool Result: {toolCall.Name} -> {toolCall.Result}");
                }
            }
        }
        Console.WriteLine("===========================");
    }

    private AssistantMessage toAssistantMessage(ILlmResponse response)
    {
        var toolCallRequests = response.ToolCalls.Select(tc => new ToolCallRequest
        {
            Id = tc.Id,
            Name = tc.Name,
            Arguments = tc.Arguments
        }).ToList();
        
        return new AssistantMessage 
        { 
            Text = response.Content,
            ToolCallRequests = toolCallRequests
        };
    }

    public List<ChatMessage> InitMessages()
    {
        var systemPrompt = """
        You are a helpful assistant that can help with debugging a program.

        You will have tools to help you accomplish the user's goals.
        You can call tools up to {MaxTurns} times before you respond to the user. 

        """.Replace("{MaxTurns}", MaxTurns.ToString());
        
        return new List<ChatMessage> {
            new ChatMessage { Role = ChatMessageRole.System, Text = systemPrompt }
        };
    }

}

