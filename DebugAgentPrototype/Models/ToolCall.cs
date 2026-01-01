namespace DebugAgentPrototype.Models;

public class ToolCallRequest {
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}

public class ToolCall {
    public ToolCallRequest Request { get; init; }
    public object? Result { get; init; }
}

