using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services.tools;

namespace DebugAgentPrototype.Services;

public class AgentService(
    OpenRouterService openRouterService,
    AppState appState,
    ToolsService toolsService)
{
    private const int ToolCallLoopMaxTurns = 10;

    public event EventHandler<Message>? MessageAdded;

    private static bool IsTaskComplete(AssistantMessage assistantMessage)
    {
        return assistantMessage.ToolCallRequests.Count == 0 && !string.IsNullOrEmpty(assistantMessage.Text);
    }

    private static bool IsMaxTurnsReached(List<Message> messages)
    {
        var lastUserMessageIndex = messages.FindLastIndex(m => m.Role == MessageRole.User);
    
        var assistantMessagesAfterLastUserCount = messages
            .Skip(lastUserMessageIndex + 1).Count(m => m.Role == MessageRole.Assistant);
        
        var isMaxTurnsReached = assistantMessagesAfterLastUserCount > ToolCallLoopMaxTurns;
        Console.WriteLine($"Is max turns reached: {isMaxTurnsReached}");
        return isMaxTurnsReached;
    }

    public void AddUserMessage(string userText)
    {
        var newMessage = new UserMessage(userText) ;
        appState.Messages.Add(newMessage);
        MessageAdded?.Invoke(this, newMessage);
    }

    public async Task ProcessLastUserMessageAsync() {
        var tools = ToolsService.GetTools();

        AssistantMessage assistantMessage;

        do {
            var response = await openRouterService.CallModelAsync(appState.Messages, tools);

            assistantMessage = ToAssistantMessage(response);
            appState.Messages.Add(assistantMessage);
            MessageAdded?.Invoke(this, assistantMessage);

            if (assistantMessage.ToolCallRequests.Count > 0) {
                var toolCalls = await toolsService.CallToolsAsync(assistantMessage.ToolCallRequests);
                var toolCallMessage = new ToolCallMessage { ToolCalls = toolCalls };
                appState.Messages.Add(toolCallMessage);
                MessageAdded?.Invoke(this, toolCallMessage);
            }
        } while (!IsTaskComplete(assistantMessage) && !IsMaxTurnsReached(appState.Messages)); //TODO the assistant can both call tools and respond to user. double check if it's considered in both conditions
    
    }

    private static AssistantMessage ToAssistantMessage(ILlmResponse response)
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

    public static List<Message> InitMessages()
    {
        var fileName = SourceCodeService.GetInspectedFilePath(); //TODO fix string interpolation 
        var systemPrompt = """
        You are a helpful assistant that can help with debugging a program with LLDB debugger.

        You will have tools to help you accomplish the user's goals. The file you are inspecting is {fileName}.

        Sometimes the program will ask for input. You can use the stdin tool to provide input to the program. 
        Provide the input in the format the program expects. For example if the program expects a number, you should provide a number without any other text.
        The tool will return the output of the program after the input is provided.

    
        You can call tools up to {MaxTurns} times before you respond to the user. 

        """.Replace("{MaxTurns}", ToolCallLoopMaxTurns.ToString()); //TODO maybe add a tool to call tools?
        
        return [new SystemMessage(systemPrompt)];
    }

}

