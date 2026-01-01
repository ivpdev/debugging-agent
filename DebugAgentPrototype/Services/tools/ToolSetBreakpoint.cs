using System;
using System.Text.Json;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;

namespace DebugAgentPrototype.Services.tools;

public class ToolSetBreakpoint(AppState appState, LldbService lldbService)
{
    public static ToolConfig GetConfig()
    {
        return new ToolConfig("set_breakpoint", "Set a breakpoint at the specified line number in the source file. The breakpoint will pause execution when that line is reached.", new { 
            type = "object",
            properties = new {
                line = new { 
                    type = "integer", 
                    description = "The line number where to set the breakpoint (must be a positive integer)."
                }
            },
            required = new[] { "line" },
            additionalProperties = false
        });
    }

    private static int ParseLineFromParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            throw new ArgumentException("Parameters cannot be empty for set_breakpoint tool");
        }

        using var jsonDoc = JsonDocument.Parse(parameters);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("line", out var lineElement))
        {
            throw new ArgumentException("Missing required 'line' parameter");
        }

        if (!lineElement.TryGetInt32(out var line) || line <= 0)
        {
            throw new ArgumentException("'line' must be a positive integer");
        }

        return line;
    }

    public async Task<string> CallAsync(string parameters)
    {
        try
        {
            var line = ParseLineFromParameters(parameters);
            var command = $"breakpoint set --line {line}";
            await lldbService.SendCommandAsync(command);
            
            await Task.Delay(1000);
            return appState.LldbOutput;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON parameters: {ex.Message}", ex);
        }
    }
}

