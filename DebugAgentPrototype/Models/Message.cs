using System;
using System.Collections.Generic;
using DebugAgentPrototype.Services;

namespace DebugAgentPrototype.Models;

public enum MessageRole
{
    User,
    Assistant,
    System,
    Tool,
}

public abstract class Message
{
    public MessageRole Role { get; init; }

    public DateTime Timestamp { get; } = DateTime.Now;
}

public class AssistantMessage : Message
{
    public List<ToolCallRequest> ToolCallRequests { get; init; } = new List<ToolCallRequest>();
    public string? Text { get; init; }
    public AssistantMessage()
    {
        Role = MessageRole.Assistant;
    }
}

public class ToolCallMessage: Message
{
    public List<Services.ToolCall> ToolCalls { get; init; } = new List<Services.ToolCall>();
    public ToolCallMessage()
    {
        Role = MessageRole.Tool;
    }
}

public class UserMessage: Message
{
    public string Text;
    public UserMessage(string text)
    {
        Text = text;
        Role = MessageRole.User;
    }
}

public class SystemMessage: Message
{
    public string Text { get; init; }
    public SystemMessage(string text)
    {
        Text = text;
        Role = MessageRole.System;
    }
}
