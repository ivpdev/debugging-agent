using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolsService
{
    public static List<ToolConfig> GetTools()
    {
        return new List<ToolConfig>
        {
            ToolRun.GetConfig(),
            ToolSetBreakpoint.GetConfig(),
            ToolGetSourceCode.GetConfig(),
            new ToolConfig("continue", "Continue the execution of the program", new { type = "object", properties = new { } })
            };
    }

    private static async Task<ToolCall> callTool(ToolCallRequest toolCallRequest, AppState state, LldbService lldbService, CancellationToken ct)
    {
        object? result;
        switch (toolCallRequest.Name)
        {
            case "run": 
                result = await ToolRun.CallAsync(state, lldbService, ct);
                break;
            case "breakpoint":
                result = await ToolSetBreakpoint.CallAsync(toolCallRequest.Arguments, state, lldbService, ct);
                break;
            case "get_source_code":
                result = ToolGetSourceCode.CallAsync(state, lldbService, ct);
                break;
            case "continue":
                // TODO: Implement continue execution tool
                result = "Execution continued";
                break;
            default:
                throw new Exception($"Tool {toolCallRequest.Name} not found");
        }
        
        return new ToolCall
        {
            Id = toolCallRequest.Id,
            Name = toolCallRequest.Name,
            Arguments = toolCallRequest.Arguments,
            Result = result
        };
    }

    public static async Task<List<ToolCall>> callToolsAsync(List<ToolCallRequest> toolCallRequests, AppState state, LldbService lldbService, CancellationToken ct)
    {
        var toolCalls = new List<ToolCall>();
        foreach (var toolCallRequest in toolCallRequests) {
            var toolCall = await callTool(toolCallRequest, state, lldbService, ct);
            toolCalls.Add(toolCall);
        }
        return toolCalls;
    }
}

public class ToolConfig
{
    public string Name { get; }
    public string? Description { get; }
    public object Parameters { get; }

    public ToolConfig(string name, string description, object parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}

public class ToolCallRequest {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

//FIXME smell
public class ToolCall: ToolCallRequest {
    public object? Result { get; set; } = null;
}
