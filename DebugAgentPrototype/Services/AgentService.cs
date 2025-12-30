using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public event EventHandler<Message>? MessageAdded;

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

    private bool IsMaxTurnsReached(List<Message> messages)
    {
        var lastUserMessageIndex = messages.FindLastIndex(m => m.Role == MessageRole.User);
    
        var assistantMessagesAfterLastUserCount = messages
            .Skip(lastUserMessageIndex + 1)
            .Where(m => m.Role == MessageRole.Assistant).Count();
        
        var isMaxTurnsReached = assistantMessagesAfterLastUserCount > MaxTurns;
        Console.WriteLine($"Is max turns reached: {isMaxTurnsReached}");
        return isMaxTurnsReached;
    }

    public void AddUserMessage(string userText)
    {
        var newMessage = new UserMessage(userText) ;
        _appState.Messages.Add(newMessage);
        MessageAdded?.Invoke(this, newMessage);
    }

    public async Task ProcessLastUserMessageAsync() {
        var tools = ToolsService.GetTools();

        AssistantMessage assistantMessage;

        do {
            var response = await _openRouterService.CallModelAsync(_appState.Messages, tools);

            assistantMessage = ToAssistantMessage(response);
            _appState.Messages.Add(assistantMessage);
            MessageAdded?.Invoke(this, assistantMessage);

            if (assistantMessage.ToolCallRequests.Count > 0) {
                var toolCalls = await _toolsService.callToolsAsync(assistantMessage.ToolCallRequests);
                var toolCallMessage = new ToolCallMessage { ToolCalls = toolCalls };
                _appState.Messages.Add(toolCallMessage);
                MessageAdded?.Invoke(this, toolCallMessage);
            }
        } while (!IsTaskComplete(assistantMessage) && !IsMaxTurnsReached(_appState.Messages)); //TODO the assistant can both call tools and respond to user. double check if it's considered in both conditions
    
    }

    private void PrintMessageHistory(List<Message> messages)
    {
        Console.WriteLine("=== Full Message History ===");
        foreach (var msg in messages)
        {
            if (msg is UserMessage userMessage)
            {
                Console.WriteLine($"  User Message: {userMessage.Text}");
            }
            if (msg is SystemMessage systemMessage)
            {
                Console.WriteLine($"  System Message: {systemMessage.Text}");
            }
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

    private AssistantMessage ToAssistantMessage(ILlmResponse response)
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

    public List<Message> InitMessages()
    {
        var fileName = SourceCodeService.GetInspectedFilePath(); //TODO fix string interpolation 
        var systemPrompt = """
        You are a helpful assistant that can help with debugging a program with LLDB debugger.

        You will have tools to help you accomplish the user's goals. The file you are inspecting is {fileName}.

        Sometimes the program will ask for input. You can use the stdin tool to provide input to the program. 
        Provide the input in the format the program expects. For example if the program expects a number, you should provide a number without any other text.
        The tool will return the output of the program after the input is provided.

    
        You can call tools up to {MaxTurns} times before you respond to the user. 

        """.Replace("{MaxTurns}", MaxTurns.ToString()); //TODO maybe add a tool to call tools?
        
        return new List<Message> {
            new SystemMessage(systemPrompt)
        };
    }

}

