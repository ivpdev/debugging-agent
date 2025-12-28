using System;
using System.Collections.Generic;
using DebugAgentPrototype.Services;

namespace DebugAgentPrototype.Models;

public enum ChatMessageRole
{
    User,
    Assistant,
    System,
    Tool,
}

public class ChatMessage
{
    public ChatMessageRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;  
}

public class AssistantMessage : ChatMessage
{
    public List<ToolCallRequest> ToolCallRequests { get; set; } = new List<ToolCallRequest>();
    public int AgentLoopTurnNumber { get; set; } = 0;
    public bool IsFinalAgentLoopTurn { get; set; } = false;
    public AssistantMessage()
    {
        Role = ChatMessageRole.Assistant;
    }
}

public class ToolCallMessage: ChatMessage
{
    public List<Services.ToolCall> ToolCalls { get; set; } = new List<Services.ToolCall>();
    public ToolCallMessage()
    {
        Role = ChatMessageRole.Tool;
    }
}
