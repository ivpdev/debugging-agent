namespace DebugAgentPrototype.Services.tools;

public class ToolGetSourceCode
{
    public static ToolConfig GetConfig()
    {
        return new ToolConfig("get_source_code", "Get the source code of the program you inspect. Each line is prefixed with the line number", new { type = "object", properties = new { } });
    }

    public static string CallAsync()
    {
        return SourceCodeService.GetInspectedFileContent();
    }
}

