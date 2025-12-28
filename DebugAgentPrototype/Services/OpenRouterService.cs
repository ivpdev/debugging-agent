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
    private readonly string _apiKey;
    private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterService()
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/your-repo");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "Debug Agent Prototype");
        Console.WriteLine($"API Key loaded: {_apiKey}");
    }

    
    public async Task<ILlmResponse> CallModelAsync(List<ChatMessage> messages, List<ToolConfig>? tools = null)
    {
        Console.WriteLine("!Calling OpenRouter API with messages: " + messages.Count + " and tools: " + (tools?.Count ?? 0));

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("OPENROUTER_API_KEY environment variable is not set");
        }

        var requestBody = new
        {
            model = "openai/gpt-4o-mini", // Default model, can be made configurable
            messages = FlattenMessages(messages.Select(toOpenRouterMessage)),
            tools = tools?.Select(toOpenRouterTool).ToList()
        };

        try
        {
            // Debug: Print request body
            var requestBodyJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("=== REQUEST BODY ===");
            Console.WriteLine(requestBodyJson);
            Console.WriteLine("===================");

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody);
            
            // Debug: Print full response information
            Console.WriteLine("=== RESPONSE INFO ===");
            Console.WriteLine($"Status Code: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Reason Phrase: {response.ReasonPhrase}");
            Console.WriteLine("Headers:");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
            if (response.Content.Headers != null)
            {
                Console.WriteLine("Content Headers:");
                foreach (var header in response.Content.Headers)
                {
                    Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response Body:");
            Console.WriteLine(responseBody);
            Console.WriteLine("===================");

            response.EnsureSuccessStatusCode();

            var result = ParseOpenRouterResponse(responseBody);
            
            if (result == null)
            {
                throw new Exception("Failed to parse OpenRouter API response");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to call OpenRouter API: {ex.Message}", ex);
        }
    }

    private object toOpenRouterTool(ToolConfig tool) {
        return new {
            type = "function",
            function = new {
                name = tool.Name,
                description = tool.Description,
                parameters = tool.Parameters
            }
        };
    }

    private object toOpenRouterMessage(ChatMessage message) {
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
            var toolMessages = toolMsg.ToolCalls.Select(tc => new Dictionary<string, object>
            {
                ["role"] = "tool",
                ["content"] = JsonSerializer.Serialize(tc.Result ?? ""),
                ["tool_call_id"] = tc.Id
            }).ToList();
            
            return toolMessages;
        }
        
        return new Dictionary<string, object>
        {
            ["role"] = message.Role.ToString().ToLowerInvariant(),
            ["content"] = message.Text
        };
    }

    //TODO simplify this
    private List<object> FlattenMessages(IEnumerable<object> messageObjects)
    {
        var result = new List<object>();
        foreach (var msg in messageObjects)
        {
            if (msg is System.Collections.IEnumerable enumerable && !(msg is string) && !(msg is Dictionary<string, object>))
            {
                foreach (var item in enumerable)
                {
                    result.Add(item);
                }
            }
            else
            {
                result.Add(msg);
            }
        }
        return result;
    }

    private ILlmResponse ParseOpenRouterResponse(string jsonResponse)
    {
        using var document = JsonDocument.Parse(jsonResponse);
        var root = document.RootElement;

        // Get the choices array
        if (!root.TryGetProperty("choices", out var choicesElement) || choicesElement.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Invalid response: missing or invalid 'choices' array");
        }

        if (choicesElement.GetArrayLength() == 0)
        {
            throw new Exception("No response from OpenRouter API: empty choices array");
        }

        // Get the first choice
        var firstChoice = choicesElement[0];
        
        // Get the message from the choice
        if (!firstChoice.TryGetProperty("message", out var messageElement))
        {
            throw new Exception("Invalid response: missing 'message' in choice");
        }

        // Extract content (can be null or empty)
        var content = messageElement.TryGetProperty("content", out var contentElement) 
            ? contentElement.GetString() ?? "" 
            : "";

        // Extract tool_calls (snake_case property name)
        var toolCalls = new List<IToolCall>();
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

                    toolCalls.Add(new Models.ToolCall
                    {
                        Id = id,
                        Name = name,
                        Arguments = arguments
                    });
                }
            }
        }

        return new Models.LlmResponse
        {
            Content = content,
            ToolCalls = toolCalls
        };
    }

    private class OpenRouterResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
    }

    private class ToolCall
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public ToolCallFunction? Function { get; set; }
    }

    private class ToolCallFunction
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }
}