using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;

namespace Evals;

public class EvalService
{
    private class Eval(EvalDefinition definition, EvalResult result)
    {
        public readonly EvalDefinition Definition = definition;
        public readonly EvalResult Result = result;
    }

    private class EvalDefinition(string name, string userInput, string expected)
    {
        public readonly string Name = name;
        public readonly string UserInput = userInput;
        public readonly string Expected = expected;
    }

    private class EvalResult(List<Message> conversation, List<Message> conversationForEvaluation, string judgement)
    {
        public readonly List<Message> Conversation = conversation;
        //convesation without the information the evalator is not allowed to know (e.g. the system prompt)
        public readonly List<Message> ConversationForEvaluation = conversationForEvaluation;
        public readonly string Judgement = judgement;
    }

    private OpenRouterService _openRouterService;
    private AppState _appState;
    private LldbService _lldbService;
    private AgentService _agentService;
    private const string EvalsBasePath = "evals/evals";

    public EvalService()
    {
        InitAssistantHeadless();
    }

    private void InitAssistantHeadless()
    {
        _openRouterService = new OpenRouterService();
        _appState = new AppState();
        _lldbService = new LldbService(_appState);
        var toolsService = new ToolsService(_appState, _lldbService);
        _agentService = new AgentService(_lldbService, _openRouterService, _appState, toolsService);
    }

    public async Task RunAllEvalsAsync()
    {
        var evalDefinitions = await ReadEvalDefinitionsAsync();

        var evals = await Task.WhenAll(evalDefinitions.Select(async definition =>
        {
            var result = await RunEvalAsync(definition);
            return new Eval(definition, result);
        }));

        await WriteToFileSystemAsync(evals);
        PrintResults(evals);
    }

    public async Task RunEvalByNameAsync(string evalName)
    {
        var evalDefinition = await ReadEvalDefinitionByNameAsync(evalName);
        if (evalDefinition == null)
        {
            throw new ArgumentException($"Eval '{evalName}' not found");
        }

        var result = await RunEvalAsync(evalDefinition);
        var eval = new Eval(evalDefinition, result);
        await WriteToFileSystemAsync([eval]);
    }

    private async Task<EvalResult> RunEvalAsync(EvalDefinition definition)
    {
        var conversation = await GenerateConversationAsync(definition.UserInput);
        var conversationForEvaluation = CleanConversationForEvaluation(conversation);
        var judgement = await EvaluateConversationAsync(conversationForEvaluation, definition.Expected);
        return new EvalResult(conversation, conversationForEvaluation, judgement);
    }

    private async Task<List<Message>> GenerateConversationAsync(string userMessage)
    {
        InitAssistantHeadless();
        await _lldbService.InitializeAsync();
        _appState.Messages = _agentService.InitMessages();
        _agentService.AddUserMessage(userMessage);
        await _agentService.ProcessLastUserMessageAsync();
        return _appState.Messages;
    }

    private List<Message> CleanConversationForEvaluation(List<Message> conversation)
    {
        return conversation.Where(m => m.Role != MessageRole.System).ToList();
    }

    private async Task<string> EvaluateConversationAsync(List<Message> conversationForEvaluation,
        string expectedCriteria)
    {
        var judgePrompt = BuildJudgePrompt(conversationForEvaluation, expectedCriteria);
        var judgeMessages = new List<Message>
        {
            new SystemMessage(
                "You are an evaluation judge. Analyze the chat history and determine if the evaluation criteria are met. Your response must start with either PASS or FAIL. If FAIL, provide reasoning on a new line."),
            new UserMessage(judgePrompt)
        };

        var response = await _openRouterService.CallModelAsync(judgeMessages, null);
        return response.Content ?? "FAIL\n\nJudge did not return a response";
    }

    private string BuildJudgePrompt(List<Message> conversationForEvaluation, string expectedCriteria)
    {
        return $@"Evaluate the following chat history against the criteria:

                ## Expected Criteria:
                {expectedCriteria}

                ## Chat History:
                {ToString(conversationForEvaluation)}

                Your response must start with PASS or FAIL. If FAIL, provide reasoning on a new line.";
    }

    private async Task<List<EvalDefinition>> ReadEvalDefinitionsAsync()
    {
        var evalDefinitions = new List<EvalDefinition>();
        var evalsPath = GetEvalsDirectory();

        var evalFolders = Directory.GetDirectories(evalsPath);
        foreach (var evalFolder in evalFolders)
        {
            var evalName = Path.GetFileName(evalFolder);
            var userInput = await File.ReadAllTextAsync(Path.Combine(evalFolder, "user-input.md"));
            var expected = await File.ReadAllTextAsync(Path.Combine(evalFolder, "expected.md"));
            var evalDefinition = new EvalDefinition(evalName, userInput, expected);
            evalDefinitions.Add(evalDefinition);
        }

        return evalDefinitions;
    }

    private async Task<EvalDefinition?> ReadEvalDefinitionByNameAsync(string evalName)
    {
        var evalDefinitions = await ReadEvalDefinitionsAsync();
        return evalDefinitions.FirstOrDefault(e => e.Name == evalName);
    }

    
    private async Task WriteToFileSystemAsync(Eval[] evals)
    {
        var evalsPath = GetEvalsDirectory();

        foreach (var eval in evals)
        {
            var evalFolder = Path.Combine(evalsPath, eval.Definition.Name);
            await WriteToFileSystemAsync(eval, evalFolder);
        }
    }

     private async Task WriteToFileSystemAsync(Eval eval, string evalFolder) {
        var chatHistoryPath = Path.Combine(evalFolder, "chat-history.json");
        var resultPath = Path.Combine(evalFolder, "result.md");
        await File.WriteAllTextAsync(chatHistoryPath, ToJsonString(eval.Result.Conversation));
        await File.WriteAllTextAsync(resultPath, eval.Result.Judgement);
     }

private string GetEvalsDirectory()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), EvalsBasePath);
        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new Exception("Evals directory not found");
        }
        return fullPath;
    }

    private string ToString(List<Message> conversationForEvaluation)
    {
        var sb = new StringBuilder();
        foreach (var message in conversationForEvaluation)
        {
            sb.AppendLine(ToString(message));
        }

        return sb.ToString();
    }

    private string ToString(Message message)
    {
        return ToJsonString(SerializeMessage(message));
    }

    private string ToJsonString(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private object SerializeMessage(Message message)
    {
        var baseMessage = new Dictionary<string, object>
        {
            ["Role"] = message.Role.ToString(),
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

    private void PrintResults(Eval[] evals)
    {
        foreach (var eval in evals)
        {
            var judgement = eval.Result.Judgement;
            var passed = judgement.StartsWith("PASS", StringComparison.OrdinalIgnoreCase);
            var status = passed ? "PASSED" : "FAILED";
            Console.WriteLine($"{status} {eval.Definition.Name}");
        }
    }
}