using System.Collections.Generic;

namespace DebugAgentPrototype.Models;

public class AgentResult
{
    public string AssistantReplyText { get; set; } = string.Empty;
    public List<IToolCall>? ToolCalls { get; set; }
}

