using System.Collections.Generic;

namespace DebugAgentPrototype.Models;

public interface ILlmResponse {
    string Content { get; }
    List<IToolCall> ToolCalls { get; }
}

public interface IToolCall {
    string Id { get; }
    string Name { get; }
    string Arguments { get; }
}

public class LlmResponse : ILlmResponse
{
    public string Content { get; set; } = "";
    public List<IToolCall> ToolCalls { get; set; } = new List<IToolCall>();
}

public class ToolCall : IToolCall
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = "";
}