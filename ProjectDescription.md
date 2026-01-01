
# DebugAgentPrototype - AI-Assisted Debugger

## Overview
Desktop GUI prototype for an AI-assisted debugger using Avalonia UI + ReactiveUI + MVVM. Currently a scaffolding with command-based agent simulation (no real AI/debugger integration yet).

IMPORTANT The code is not meant to be production-ready. The intention of this prototype is to show the solution design. Technical aspects like handling or edge cases, proper error handling, logging etc. were omitted for sake of readbility.

## Tech Stack
- **Framework**: .NET 9.0
- **UI**: Avalonia 11.0.0 (cross-platform desktop)
- **MVVM**: ReactiveUI (reactive bindings, commands)
- **Pattern**: MVVM with code-behind only for InitializeComponent

## Architecture

### Project Structure
```
Models/          - Data models (ChatMessage, Breakpoint, StackFrame, AgentResult, DebugOutput, AppState)
Services/        - Business logic (AgentService, DebuggerService)
ViewModels/      - MainViewModel (ReactiveObject with ObservableCollections)
Views/           - MainWindow.axaml + code-behind
```

### UI Layout
- **Left**: Chat interface (scrollable message list + input with Enter-to-send)
- **Right**: Call Stack panel (displays StackFrame list)

### Data Flow
1. User types message → `MainViewModel.SendMessageAsync()`
2. Adds user message to `Messages` collection immediately
3. Calls `AgentService.HandleAsync(userText, AppState, ct)`
4. Agent processes command → returns `AgentResult`
5. UI updates: adds agent reply, updates breakpoints/call stack

### Key Components

**MainViewModel** (`ViewModels/MainViewModel.cs`)
- Manages `ObservableCollection<ChatMessage> Messages`
- Manages `ObservableCollection<StackFrame> CallStack`
- Manages `ObservableCollection<Breakpoint> Breakpoints`
- `ReactiveCommand SendMessageCommand` (enabled when not busy + input not empty)
- Uses `Dispatcher.UIThread.Post()` for thread-safe collection updates

**AgentService** (`Services/AgentService.cs`)
- `Task<AgentResult> HandleAsync(string userText, AppState state, CancellationToken ct)`
- Currently implements 3 commands:
  - `echo {text}` → responds `answer {text}`
  - `breakpoint {line}` → validates line > 0, adds to state.Breakpoints
  - `debug` → calls DebuggerService, returns console output + call stack
- Unknown commands → returns help text
- **Integration point**: Replace command parsing with real AI provider call

**DebuggerService** (`Services/DebuggerService.cs`)
- `Task<DebugOutput> RunAsync(IReadOnlyList<Breakpoint> breakpoints, CancellationToken ct)`
- Currently returns stub data (console output string + 3-frame call stack)
- **Integration point**: Replace with real debugger API calls

**AppState** (`Models/AppState.cs`)
- Holds `List<Breakpoint> Breakpoints` and `List<StackFrame> CurrentCallStack`
- Shared between AgentService and ViewModel

## Current State
- ✅ Full UI scaffolding with MVVM architecture
- ✅ Command parsing (echo, breakpoint, debug)
- ✅ Thread-safe UI updates
- ✅ Enter key handling
- ❌ No real AI integration (AgentService is command-based)
- ❌ No real debugger integration (DebuggerService is a stub)

## Integration Points

**To add real AI:**
- Replace command parsing in `AgentService.HandleAsync()` (lines 22-79)
- Keep method signature: `Task<AgentResult> HandleAsync(string userText, AppState state, CancellationToken ct)`
- Return `AgentResult` with `AssistantReplyText`, optional `BreakpointAdded`, optional `CallStack`

**To add real debugger:**
- Replace stub in `DebuggerService.RunAsync()` (lines 12-44)
- Keep method signature: `Task<DebugOutput> RunAsync(IReadOnlyList<Breakpoint> breakpoints, CancellationToken ct)`
- Return `DebugOutput` with `ConsoleOutput` string and `CallStack` list

## Models
- `ChatMessage`: Role (User/Agent), Text, Timestamp
- `Breakpoint`: Line (int)
- `StackFrame`: Function, File, Line + `DisplayText` property
- `AgentResult`: AssistantReplyText, BreakpointAdded?, CallStack?
- `DebugOutput`: ConsoleOutput, CallStack
- `AppState`: Breakpoints, CurrentCallStack


TODO 
development approach EDD
clean AI slop from this description
directions
  - context compression
  - cost & latency optimization
    - model tweaking, fallback
  - source code
    - smart tools for browsing through the code base


TODO Code
  - TODO class vs interface
  - clean logging
  - TODO clean the readme
  - privatte methods convention 
  - add the most recent debugger ouput to every call
  - context compression
  - remove cancellation token
  - make file
  - fix rerendering of the history (scrolly is reset)
  - try to remove tools
  - static methods and singletones
  - wiring of the app
  - how app state is managed
  - fix warnings
  - review flattened tool call history
  - clean console output
  - clean unsused models
  - move models close to logic
  - check Rider
  - check edge cases on the agent loop max turns
    - the UI is rendered correctly
    - evals work
    - does assistant understands what to do with it?
  - add debugger output
  - fix .env
  - test running from scratch
  - extract evals