using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class AgentService(
    OpenRouterService openRouterService,
    AppState appState,
    ToolsService toolsService,
    LldbService lldbService)
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

        Console.WriteLine($"Assistant messages after last user count: {assistantMessagesAfterLastUserCount}");
        var isMaxTurnsReached = assistantMessagesAfterLastUserCount > ToolCallLoopMaxTurns;
        Console.WriteLine($"Is max turns reached: {isMaxTurnsReached}");
        return isMaxTurnsReached;
    }

    public void AddUserMessage(string userText)
    {
        var newMessage = new UserMessage(userText);
        appState.Messages.Add(newMessage);
        MessageAdded?.Invoke(this, newMessage);
    }

    public async Task SendUserLldbCommandAsync(string command)
    {
        await lldbService.SendCommandAsync(command);
        var lldbOutput = appState.LldbOutput;

        var newMessage = new UserLldbCommandMessage(command, lldbOutput);
        appState.Messages.Add(newMessage);
        MessageAdded?.Invoke(this, newMessage);
    }

    public async Task ProcessLastUserMessageAsync() {
        var tools = ToolsService.GetTools();

        AssistantMessage assistantMessage;

        do {
            assistantMessage = await openRouterService.CallModelAsync(appState.Messages, tools);
            appState.Messages.Add(assistantMessage);
            MessageAdded?.Invoke(this, assistantMessage);
            
            if (assistantMessage.ToolCallRequests.Count > 0) {
                var toolCalls = await toolsService.CallToolsAsync(assistantMessage.ToolCallRequests);
                var toolCallMessages = toolCalls.Select(toolCall => new ToolCallMessage(toolCall)).ToList();
                foreach (var toolCallMessage in toolCallMessages)
                {
                    appState.Messages.Add(toolCallMessage);
                    MessageAdded?.Invoke(this, toolCallMessage);
                }
            }
        } while (!IsTaskComplete(assistantMessage) && !IsMaxTurnsReached(appState.Messages));
    
    }

    public static List<Message> InitMessages()
    {
        var systemPrompt = GetSystemPrompt();
        return [new SystemMessage(systemPrompt)];
    }

    private static string GetSystemPrompt()
    {
        return $"""
        You are a helpful assistant that can the user help with debugging a program with LLDB debugger.

        You will have tools to help you accomplish the user's goals.
        
        Sometimes the program will ask for input. You can use the stdin_write tool to provide input to the program. 
        Provide the input in the format the program expects. For example if the program expects a number, you should provide a number without any other text.
        The tool will return the output of the program after the input is provided.

        When setting breakpoint do not include the file name in the command.
        
        You can call tools up to {ToolCallLoopMaxTurns} times before you respond to the user. 
        """;
    }

}

