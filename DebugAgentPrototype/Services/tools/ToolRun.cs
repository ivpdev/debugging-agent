using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public static class ToolRun
{
    public static ToolConfig GetConfig()
    {
        return new ToolConfig("run", "Run the program", new { type = "object", properties = new { } });
    }

    public static async Task<string> CallAsync(AppState state, LldbService lldbService, CancellationToken ct)
    {
        await lldbService.StartAsync(state.Breakpoints, ct);
        return "Program run";
    }
}

