using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolSetBreakpoint
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;

    public ToolSetBreakpoint(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
    }

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

    public async Task<string> CallAsync(string parameters, CancellationToken ct)
    {
        try
        {
            int line = ParseLineFromParameters(parameters);
            _appState.Breakpoints.Add(new Breakpoint(line));

            //TODO do we need IsRunning check here?


            if (_lldbService.IsRunning) //TODO check if breakpoinst pre run are picked up
            {
                await _lldbService.SendCommandAsync($"br set --file game.c --line {line}", ct);
                await Task.Delay(1000, ct);
            }

            return $"Breakpoint set at line {line}";
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON parameters: {ex.Message}", ex);
        }
    }
}

