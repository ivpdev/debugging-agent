using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolsService
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;
    private readonly ToolGetSourceCode _toolGetSourceCode;
    private readonly ToolStdin _toolStdin;

    public ToolsService(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
        _toolGetSourceCode = new ToolGetSourceCode(_appState, _lldbService);
        _toolStdin = new ToolStdin(_appState, _lldbService);
    }

    public static List<ToolConfig> GetTools()
    {
        return new List<ToolConfig>
        {
            ToolGetSourceCode.GetConfig(),
            ToolStdin.GetConfig()
        };
    }

    private async Task<ToolCall> callTool(ToolCallRequest toolCallRequest)
    {
        object? result;
        switch (toolCallRequest.Name)
        {
            case "get_source_code":
                result = _toolGetSourceCode.CallAsync();
                break;
            case "stdin_write":
                result = await _toolStdin.CallAsync(toolCallRequest.Arguments);
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

    public async Task<List<ToolCall>> callToolsAsync(List<ToolCallRequest> toolCallRequests)
    {
        var toolCalls = new List<ToolCall>();
        foreach (var toolCallRequest in toolCallRequests) {
            var toolCall = await callTool(toolCallRequest);
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
