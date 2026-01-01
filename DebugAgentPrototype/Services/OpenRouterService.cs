using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class OpenRouterService
{
    private readonly HttpClient _httpClient;
    //TODO separate base URL and move to config
    private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OPENROUTER_API_KEY environment variable is not set");
        }
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    public async Task<AssistantMessage> CallModelAsync(List<Message> messages, List<ToolConfig>? tools = null)
    {
        var requestBody = new
        {
            model = "anthropic/claude-sonnet-4.5", 
            messages = messages.Select(ToOpenRouterMessage),
            tools = tools?.Select(ToOpenRouterTool).ToList()
        };

        Console.WriteLine($"[OpenRouter] Request body: {JsonSerializer.Serialize(requestBody)}");

        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody);
            
            Console.WriteLine($"[OpenRouter] Response status code: {(int)response.StatusCode} {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[OpenRouter] Response body: {responseBody}");
            response.EnsureSuccessStatusCode();

            var result = ParseOpenRouterResponse(responseBody);
            
            if (result == null)
            {
                throw new Exception("Failed to parse OpenRouter API response");
            }

            return ToAssistantMessage(result);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to call OpenRouter API: {ex.Message}", ex);
        }
    }

    private static object ToOpenRouterTool(ToolConfig tool) {
        return new {
            type = "function",
            function = new {
                name = tool.Name,
                description = tool.Description,
                parameters = tool.Parameters
            }
        };
    }

    private static object ToOpenRouterMessage(Message message) {
        if (message is AssistantMessage assistantMsg)
        {
            var result = new Dictionary<string, object>
            {
                ["role"] = "assistant",
                ["content"] = assistantMsg.Text ?? ""
            };
            
            if (assistantMsg.ToolCallRequests.Count > 0)
            {
                var toolCalls = assistantMsg.ToolCallRequests.Select(tcr => new Dictionary<string, object>
                {
                    ["id"] = tcr.Id,
                    ["type"] = "function",
                    ["function"] = new Dictionary<string, object>
                    {
                        ["name"] = tcr.Name,
                        ["arguments"] = tcr.Arguments
                    }
                }).ToList();
                result["tool_calls"] = toolCalls;
            }
            
            return result;
        }

        if (message is ToolCallMessage toolMsg)
        {
            return new Dictionary<string, object>
            {
                ["role"] = "tool",
                ["content"] = JsonSerializer.Serialize(toolMsg.ToolCall.Result ?? ""),
                ["tool_call_id"] = toolMsg.ToolCall.Request.Id
            };
        }

        if (message is UserMessage userMsg)
        {
            return new Dictionary<string, object>
            {
                ["role"] = "user",
                ["content"] = userMsg.Text
            };
        }

        if (message is UserLldbCommandMessage userLldbCommandMsg)
        {
            return new Dictionary<string, object>
            {
                ["role"] = "user",
                ["content"] = "I ran the following command: " + userLldbCommandMsg.Command + "\nHere is the output:\n" + userLldbCommandMsg.Output
            };
        }
        
        if (message is SystemMessage systemMsg)
        {
            return new Dictionary<string, object>
            {
                ["role"] = "system",
                ["content"] = systemMsg.Text
            };
        }

        throw new Exception($"Unknown message type: {message.GetType()}");
    }
    
    private static OpenRouterResponse ParseOpenRouterResponse(string jsonResponse)
    {
        using var document = JsonDocument.Parse(jsonResponse);
        var root = document.RootElement;

        if (!root.TryGetProperty("choices", out var choicesElement) || choicesElement.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Invalid response: missing or invalid 'choices' array");
        }

        if (choicesElement.GetArrayLength() == 0)
        {
            throw new Exception("No response from OpenRouter API: empty choices array");
        }

        var firstChoice = choicesElement[0];
        if (!firstChoice.TryGetProperty("message", out var messageElement))
        {
            throw new Exception("Invalid response: missing 'message' in choice");
        }

        var content = messageElement.TryGetProperty("content", out var contentElement) 
            ? contentElement.GetString() ?? "" 
            : "";

        var toolCalls = new List<OpenRouterToolCall>();
        if (messageElement.TryGetProperty("tool_calls", out var toolCallsElement) && 
            toolCallsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                var id = toolCallElement.TryGetProperty("id", out var idElement) 
                    ? idElement.GetString() ?? "" 
                    : "";
                
                if (toolCallElement.TryGetProperty("function", out var functionElement))
                {
                    var name = functionElement.TryGetProperty("name", out var nameElement) 
                        ? nameElement.GetString() ?? "" 
                        : "";
                    
                    var arguments = functionElement.TryGetProperty("arguments", out var argsElement) 
                        ? argsElement.GetString() ?? "" 
                        : "";

                    toolCalls.Add(new OpenRouterToolCall
                    {
                        Id = id,
                        Name = name,
                        Arguments = arguments
                    });
                }
            }
        }

        return new OpenRouterResponse
        {
            Content = content,
            ToolCalls = toolCalls
        };
    }

    private static AssistantMessage ToAssistantMessage(OpenRouterResponse response)
    {
        var toolCallRequests = response.ToolCalls.Select(tc => new ToolCallRequest
        {
            Id = tc.Id,
            Name = tc.Name,
            Arguments = tc.Arguments
        }).ToList();
        
        return new AssistantMessage 
        { 
            Text = response.Content,
            ToolCallRequests = toolCallRequests
        };
    }

    private class OpenRouterResponse
    {
        public string Content { get; set; } = "";
        public List<OpenRouterToolCall> ToolCalls { get; set; } = new List<OpenRouterToolCall>();
    }

    private class OpenRouterToolCall
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}