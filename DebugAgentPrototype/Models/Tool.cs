namespace DebugAgentPrototype.Models;

public class ToolCallRequest {
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}

public class ToolCall {
    public ToolCallRequest Request { get; init; } = null!;
    public object? Result { get; init; }
}

public class ToolConfig(string name, string description, object parameters)
{
    public string Name { get; } = name;
    public string? Description { get; } = description;
    public object Parameters { get; } = parameters;
}

