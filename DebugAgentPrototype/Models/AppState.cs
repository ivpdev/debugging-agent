using System;
using System.Collections.Generic;

namespace DebugAgentPrototype.Models;

public class AppState
{
    public List<Breakpoint> Breakpoints { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();

    public List<DebuggerHistoryItem> DebuggerHistory { get; set; } = new();
}


public class DebuggerHistoryItem
{
    public string Command { get; set; }
    public string Output { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
