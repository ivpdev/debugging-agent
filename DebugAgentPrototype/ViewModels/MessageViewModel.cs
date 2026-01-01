using System.Collections.Generic;
using System.Text.Json;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.ViewModels;

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

