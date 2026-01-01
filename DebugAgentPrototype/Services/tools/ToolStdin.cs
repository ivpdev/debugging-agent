using System;
using System.Text.Json;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services.tools;

namespace DebugAgentPrototype.Services;

public class ToolStdin
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;

    public ToolStdin(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
    }

    public static ToolConfig GetConfig()
    {
        return new ToolConfig("stdin_write", "Write EXACTLY the provided text to the program's stdin. The `text` value is the literal bytes to send (e.g., \"run\\n\"). Do NOT wrap it in JSON or add keys like `command`.", new { 
            type = "object",
            properties = new {
                text = new { 
                    type = "string", 
                    description = "Literal stdin payload. Example: \"run\\n\" (not {\"command\":\"run\"})."
                }
            },
            required = new[] { "text" },
            additionalProperties = false
        });
    }

    private static string ParseTextFromParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            throw new ArgumentException("Parameters cannot be empty for stdin_write tool");
        }

        using var jsonDoc = JsonDocument.Parse(parameters);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("text", out var textElement))
        {
            throw new ArgumentException("Missing required 'text' parameter");
        }

        return textElement.GetString() ?? throw new ArgumentException("'text' must be a non-null string");
    }

    public async Task<string> CallAsync(string parameters)
    {
        try
        {
            string text = ParseTextFromParameters(parameters);
            await _lldbService.SendCommandAsync(text);
            await Task.Delay(1000);
            return _appState.LldbOutput;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON parameters: {ex.Message}", ex);
        }
    }
}

