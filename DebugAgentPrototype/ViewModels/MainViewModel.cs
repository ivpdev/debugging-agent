using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using ReactiveUI;

namespace DebugAgentPrototype.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly LldbService _lldbService;

    public LldbOutputViewModel LldbOutputViewModel { get; }
    public ChatViewModel ChatViewModel { get; }

    public MainViewModel(
        AgentService agentService,
        AppState appState,
        LldbService lldbService,
        LldbOutputViewModel lldbOutputViewModel,
        ChatViewModel chatViewModel)
    {
        _lldbService = lldbService;
        LldbOutputViewModel = lldbOutputViewModel;
        ChatViewModel = chatViewModel;

        appState.Messages = AgentService.InitMessages();
    }

    public async Task InitializeLldbAsync()
    {
        try
        {
            await _lldbService.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] Error initializing LLDB: {ex.Message}");
        }
    }
}

