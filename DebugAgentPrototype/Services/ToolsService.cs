using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services.tools;

namespace DebugAgentPrototype.Services;

public class ToolsService(AppState appState, LldbService lldbService)
{
    private readonly ToolStdinWrite _toolStdinWrite = new(appState, lldbService);

    public static List<ToolConfig> GetTools()
    {
        return
        [
            ToolGetSourceCode.GetConfig(),
            ToolStdinWrite.GetConfig()
        ];
    }

    private async Task<ToolCall> CallToolAsync(ToolCallRequest toolCallRequest)
    {
        object? result = toolCallRequest.Name switch
        {
            "get_source_code" => ToolGetSourceCode.CallAsync(),
            "stdin_write" => await _toolStdinWrite.CallAsync(toolCallRequest.Arguments),
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
