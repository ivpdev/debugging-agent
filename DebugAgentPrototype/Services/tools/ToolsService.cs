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
           // ToolSetBreakpoint.GetConfig() with current evals the assistant performs better using "stdin_write" as universal tool to operate the LLDB. However it should be reevaluated with more evals
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
            Request = toolCallRequest,
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

//TODO move to models
public class ToolConfig(string name, string description, object parameters)
{
    public string Name { get; } = name;
    public string? Description { get; } = description;
    public object Parameters { get; } = parameters;
}
