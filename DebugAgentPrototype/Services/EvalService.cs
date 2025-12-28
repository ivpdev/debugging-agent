using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class EvalService
{
    private readonly OpenRouterService _openRouterService;
    private const string EvalsBasePath = "evals/evals";

    public EvalService(OpenRouterService openRouterService)
    {
        _openRouterService = openRouterService;
    }

    public async Task RunAllEvalsAsync(CancellationToken ct = default)
    {
        var evalsPath = FindEvalsDirectory();
        if (evalsPath == null)
        {
            Console.WriteLine("Evals directory not found. Searched in:");
            Console.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), EvalsBasePath)}");
            Console.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), "DebugAgentPrototype", EvalsBasePath)}");
            return;
        }

        var evalFolders = Directory.GetDirectories(evalsPath);
        Console.WriteLine($"Found {evalFolders.Length} eval(s) to run");

        var results = new List<(string Name, string Status)>();

        foreach (var evalFolder in evalFolders)
        {
            var evalName = Path.GetFileName(evalFolder);
            Console.WriteLine($"\n=== Running eval: {evalName} ===");
            
            try
            {
                await RunEvalAsync(evalFolder, ct);
                var resultPath = Path.Combine(evalFolder, "result.md");
                if (File.Exists(resultPath))
                {
                    var resultContent = await File.ReadAllTextAsync(resultPath, ct);
                    var status = resultContent.TrimStart().StartsWith("PASS", StringComparison.OrdinalIgnoreCase) ? "PASS" : "FAIL";
                    results.Add((evalName, status));
                }
                else
                {
                    results.Add((evalName, "FAIL"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running eval {evalName}: {ex.Message}");
                await WriteResultAsync(evalFolder, $"FAIL\n\nError during evaluation: {ex.Message}");
                results.Add((evalName, "FAIL"));
            }
        }

        Console.WriteLine("\n=== Evaluation Summary ===");
        foreach (var (name, status) in results)
        {
            Console.WriteLine($"{name}: {status}");
        }
    }

    private async Task RunEvalAsync(string evalFolder, CancellationToken ct)
    {
        var userInputPath = Path.Combine(evalFolder, "user-input.md");
        var expectedPath = Path.Combine(evalFolder, "expected.md");
        var chatHistoryPath = Path.Combine(evalFolder, "chat-history.json");
        var resultPath = Path.Combine(evalFolder, "result.md");

        if (!File.Exists(userInputPath))
        {
            throw new FileNotFoundException($"user-input.md not found in {evalFolder}");
        }

        if (!File.Exists(expectedPath))
        {
            throw new FileNotFoundException($"expected.md not found in {evalFolder}");
        }

        var userInput = await File.ReadAllTextAsync(userInputPath, ct);
        var expectedCriteria = await File.ReadAllTextAsync(expectedPath, ct);

        Console.WriteLine("User input: " + userInput);
        Console.WriteLine("Expected criteria: " + expectedCriteria);

        var appState = new AppState();
        var lldbService = new LldbService(appState);
        var toolsService = new ToolsService(appState, lldbService);
        var agentService = new AgentService(lldbService, _openRouterService, appState, toolsService);

        appState.Messages = agentService.InitMessages();
        agentService.addUserMessage(userInput);
        await agentService.ProcessLastUserMessageAsync(ct);

        var chatHistoryWithoutSystem = appState.Messages
            .Where(m => m.Role != ChatMessageRole.System)
            .ToList();

        var serializableHistory = chatHistoryWithoutSystem.Select(m => SerializeMessage(m)).ToList();
        
        var chatHistoryJson = JsonSerializer.Serialize(serializableHistory, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(chatHistoryPath, chatHistoryJson, ct);
        Console.WriteLine($"Chat history saved to {chatHistoryPath}");

        var judgeResult = await JudgeEvalAsync(chatHistoryWithoutSystem, expectedCriteria, ct);
        await File.WriteAllTextAsync(resultPath, judgeResult, ct);
        Console.WriteLine($"Result written to {resultPath}");
    }

    private async Task<string> JudgeEvalAsync(List<ChatMessage> chatHistory, string expectedCriteria, CancellationToken ct)
    {
        var judgePrompt = BuildJudgePrompt(chatHistory, expectedCriteria);
        
        var judgeMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = ChatMessageRole.System,
                Text = "You are an evaluation judge. Analyze the chat history and determine if the evaluation criteria are met. Your response must start with either PASS or FAIL. If FAIL, provide reasoning on a new line."
            },
            new ChatMessage
            {
                Role = ChatMessageRole.User,
                Text = judgePrompt
            }
        };

        var response = await _openRouterService.CallModelAsync(judgeMessages, null);
        return response.Content ?? "FAIL\n\nJudge did not return a response";
    }

    private string BuildJudgePrompt(List<ChatMessage> chatHistory, string expectedCriteria)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Evaluate the following chat history against the criteria:");
        sb.AppendLine();
        sb.AppendLine("## Expected Criteria:");
        sb.AppendLine(expectedCriteria);
        sb.AppendLine();
        sb.AppendLine("## Chat History:");
        sb.AppendLine();

        foreach (var message in chatHistory)
        {
            sb.AppendLine($"[{message.Role}]");
            
            if (message is AssistantMessage assistantMsg)
            {
                if (!string.IsNullOrEmpty(assistantMsg.Text))
                {
                    sb.AppendLine(assistantMsg.Text);
                }
                
                if (assistantMsg.ToolCallRequests.Count > 0)
                {
                    foreach (var toolCall in assistantMsg.ToolCallRequests)
                    {
                        sb.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
                    }
                }
            }
            else if (message is ToolCallMessage toolMsg)
            {
                foreach (var toolCall in toolMsg.ToolCalls)
                {
                    var resultJson = JsonSerializer.Serialize(toolCall.Result ?? "");
                    sb.AppendLine($"Tool Result: {toolCall.Name} -> {resultJson}");
                }
            }
            else
            {
                sb.AppendLine(message.Text);
            }
            
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Your response must start with PASS or FAIL. If FAIL, provide reasoning on a new line.");

        return sb.ToString();
    }

    private async Task WriteResultAsync(string evalFolder, string result)
    {
        var resultPath = Path.Combine(evalFolder, "result.md");
        await File.WriteAllTextAsync(resultPath, result);
    }

    private string? FindEvalsDirectory()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), EvalsBasePath),
            Path.Combine(Directory.GetCurrentDirectory(), "Evals", EvalsBasePath),
            Path.Combine(Directory.GetCurrentDirectory(), "DebugAgentPrototype", EvalsBasePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", EvalsBasePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Evals", EvalsBasePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DebugAgentPrototype", EvalsBasePath)
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private object SerializeMessage(ChatMessage message)
    {
        var baseMessage = new Dictionary<string, object>
        {
            ["Role"] = message.Role.ToString(),
            ["Text"] = message.Text,
            ["Timestamp"] = message.Timestamp
        };

        if (message is AssistantMessage assistantMsg)
        {
            if (assistantMsg.ToolCallRequests.Count > 0)
            {
                baseMessage["ToolCallRequests"] = assistantMsg.ToolCallRequests.Select(tcr => new Dictionary<string, object>
                {
                    ["Id"] = tcr.Id,
                    ["Name"] = tcr.Name,
                    ["Arguments"] = tcr.Arguments
                }).ToList();
            }
        }
        else if (message is ToolCallMessage toolMsg)
        {
            if (toolMsg.ToolCalls.Count > 0)
            {
                baseMessage["ToolCalls"] = toolMsg.ToolCalls.Select(tc =>
                {
                    var toolCallDict = new Dictionary<string, object>
                    {
                        ["Id"] = tc.Id,
                        ["Name"] = tc.Name,
                        ["Arguments"] = tc.Arguments
                    };
                    
                    if (tc.Result != null)
                    {
                        try
                        {
                            var resultJson = JsonSerializer.Serialize(tc.Result);
                            toolCallDict["Result"] = resultJson;
                        }
                        catch
                        {
                            toolCallDict["Result"] = tc.Result.ToString() ?? "";
                        }
                    }
                    else
                    {
                        toolCallDict["Result"] = "";
                    }
                    
                    return toolCallDict;
                }).ToList();
            }
        }

        return baseMessage;
    }
}

