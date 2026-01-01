using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services.tools;

public class ToolsService(AppState appState, LldbService lldbService)
{
    private readonly ToolStdinWrite _toolStdinWrite = new(appState, lldbService);
    private readonly ToolSetBreakpoint _toolSetBreakpoint = new(appState, lldbService);

    public static List<ToolConfig> GetTools()
    {
        return
        [
            ToolGetSourceCode.GetConfig(),
            ToolStdinWrite.GetConfig(),
           // ToolSetBreakpoint.GetConfig()
        ];
    }

    private async Task<ToolCall> CallToolAsync(ToolCallRequest toolCallRequest)
    {
        object? result = toolCallRequest.Name switch
        {
            "get_source_code" => ToolGetSourceCode.CallAsync(),
            "stdin_write" => await _toolStdinWrite.CallAsync(toolCallRequest.Arguments),
            "set_breakpoint" => await _toolSetBreakpoint.CallAsync(toolCallRequest.Arguments),
            _ => throw new Exception($"Tool {toolCallRequest.Name} not found")
        };

        return new ToolCall
        {
            Id = toolCallRequest.Id,
            Name = toolCallRequest.Name,
            Arguments = toolCallRequest.Arguments,
            Result = result
        };
    }

    public async Task<List<ToolCall>> CallToolsAsync(List<ToolCallRequest> toolCallRequests)
    {
        var toolCalls = new List<ToolCall>();
        foreach (var toolCallRequest in toolCallRequests) {
            var toolCall = await CallToolAsync(toolCallRequest);
            toolCalls.Add(toolCall);
        }
        return toolCalls;
    }
}

public class ToolConfig(string name, string description, object parameters)
{
    public string Name { get; } = name;
    public string? Description { get; } = description;
    public object Parameters { get; } = parameters;
}

public class ToolCallRequest {
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
}

//FIXME smell
public class ToolCall: ToolCallRequest {
    public object? Result { get; init; }
}
