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
    private const int MaxTurns = 3;
    
    private readonly LldbService _lldbService;
    private readonly OpenRouterService _openRouterService;

    public event EventHandler<List<ChatMessage>>? MessagesUpdated;

    public AgentService(LldbService lldbService, OpenRouterService llmService)
    {
        _lldbService = lldbService;
        _openRouterService = llmService;
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

    public void addUserMessage(string userText, AppState state)
    {
        state.Messages.Add(new ChatMessage { Role = ChatMessageRole.User, Text = userText });
        MessagesUpdated?.Invoke(this, state.Messages);
    }

    public async Task ProcessLastUserMessageAsync(AppState state, CancellationToken ct) {
        var tools = ToolsService.GetTools();
        
        ILlmResponse response;
        AssistantMessage assistantMessage;

        do {
            response = await _openRouterService.CallModelAsync(state.Messages, tools);

            assistantMessage = toAssistantMessage(response);
            state.Messages.Add(assistantMessage);
            MessagesUpdated?.Invoke(this, state.Messages);

            if (assistantMessage.ToolCallRequests.Count > 0) {
                var toolCalls = await ToolsService.callToolsAsync(assistantMessage.ToolCallRequests, state, _lldbService, ct);
                state.Messages.Add(new ToolCallMessage { ToolCalls = toolCalls });
                MessagesUpdated?.Invoke(this, state.Messages);
            }
        } while (!IsTaskComplete(assistantMessage) && !IsMaxTurnsReached(state.Messages));
    
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
        const string systemPrompt = """
        You are a helpful assistant that can help with debugging a program.

        You will have tools to help you accomplish the user's goals.
        You can call tools up to 3 times before you respond to the user. 

        """;
        return new List<ChatMessage> {
            new ChatMessage { Role = ChatMessageRole.System, Text = systemPrompt }
        };
    }

}

