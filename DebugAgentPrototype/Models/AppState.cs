using System;
using System.Collections.Generic;

namespace DebugAgentPrototype.Models;

public class AppState
{ public List<ChatMessage> Messages { get; set; } = new();
    public string LldbOutput { get; set; } = string.Empty;
}



