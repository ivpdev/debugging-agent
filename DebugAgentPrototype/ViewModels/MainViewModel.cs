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
    public string DebuggedFileName { get; }

    public MainViewModel(AppState appState,
        LldbService lldbService,
        LldbOutputViewModel lldbOutputViewModel,
        ChatViewModel chatViewModel)
    {
        _lldbService = lldbService;
        LldbOutputViewModel = lldbOutputViewModel;
        ChatViewModel = chatViewModel;
        DebuggedFileName = SourceCodeService.GetInpectedFileName();

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

