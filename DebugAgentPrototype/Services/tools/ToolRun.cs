using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolRun
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;

    public ToolRun(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
    }

    public static ToolConfig GetConfig()
    {
        return new ToolConfig("run", "Run the program", new { type = "object", properties = new { } });
    }

    public async Task<string> CallAsync(CancellationToken ct)
    {
        await _lldbService.StartAsync(_appState.Breakpoints, ct); //TODO do we need pass breakpoints here?
        await Task.Delay(2000, ct);
        return _appState.LldbOutput;
    }
}

