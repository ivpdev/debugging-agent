
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class ToolLldbInput
{
    private readonly AppState _appState;
    private readonly LldbService _lldbService;

    public ToolLldbInput(AppState appState, LldbService lldbService)
    {
        _appState = appState;
        _lldbService = lldbService;
    }

    public static ToolConfig GetConfig()
    {
        return new ToolConfig("lldb_input", "Send an arbitrary command to the LLDB debugger session", new { type = "object", properties = new { command = new { type = "string", description = "The command to send to the LLDB debugger" } } });
    }

    public async Task<string> CallAsync(string command, CancellationToken ct)
    {
        await _lldbService.SendCommandAsync(command, ct);
        await Task.Delay(1000, ct);
        return "Command sent to LLDB debugger";
    }
}