namespace DebugAgentPrototype.Models;

public class Breakpoint
{
    public int Line { get; set; }

    public Breakpoint(int line)
    {
        Line = line;
    }
}


