using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public static class ToolSetBreakpoint
{
    public static ToolConfig GetConfig()
    {
        return new ToolConfig("breakpoint", "Set a breakpoint at the given line number", new { 
            type = "object",
            properties = new {
                line = new { 
                    type = "integer", 
                    description = "Line number where to set the breakpoint" 
                }
            },
            required = new[] { "line" }
        });
    }

    private static int ParseLineFromParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            throw new ArgumentException("Parameters cannot be empty for breakpoint tool");
        }

        using var jsonDoc = JsonDocument.Parse(parameters);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("line", out var lineElement))
        {
            throw new ArgumentException("Missing required 'line' parameter");
        }

        if (!lineElement.TryGetInt32(out int line) || line <= 0)
        {
            throw new ArgumentException("'line' must be a positive integer");
        }

        return line;
    }

    public static async Task<string> CallAsync(string parameters, AppState state, LldbService lldbService, CancellationToken ct)
    {
        try
        {
            int line = ParseLineFromParameters(parameters);
            state.Breakpoints.Add(new Breakpoint(line));

            if (lldbService.IsRunning)
            {
                await lldbService.SendCommandAsync($"br set --file game.c --line {line}", ct);
            }

            return $"Breakpoint set at line {line}";
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON parameters: {ex.Message}", ex);
        }
    }
}

