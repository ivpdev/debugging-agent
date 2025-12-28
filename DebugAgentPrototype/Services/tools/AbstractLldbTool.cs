public abstract class AbstractLldbTool
{
    public abstract ToolConfig GetConfig();
    public abstract Task<string> CallAsync(string command, AppState state, LldbService lldbService, CancellationToken ct);


}

