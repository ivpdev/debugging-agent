using System;
using System.IO;
using System.Text.Json;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolGetSourceCode
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;

    public ToolGetSourceCode(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
    }

    public static ToolConfig GetConfig()
    {
        return new ToolConfig("get_source_code", "Get the source code of the program you inspect. Each line is prefixed with the line number", new { type = "object", properties = new { } });
    }

    public string CallAsync()
    {
        return SourceCodeService.GetInspectedFileContent();
    }
}

