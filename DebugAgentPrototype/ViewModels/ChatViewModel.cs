using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using ReactiveUI;

namespace DebugAgentPrototype.ViewModels;

public class ChatViewModel : ReactiveObject
{
    private readonly AgentService _agentService;
    private readonly LldbOutputViewModel _lldbOutputViewModel;
    private string _userInput = string.Empty;
    private bool _isBusy;

    public ObservableCollection<UiMessage> UiMessages { get; }

    public ChatViewModel(
        AgentService agentService,
        LldbOutputViewModel lldbOutputViewModel)
    {
        _agentService = agentService;
        _lldbOutputViewModel = lldbOutputViewModel;
        UiMessages = [];
        
        _agentService.MessageAdded += OnMessageAdded;

        var canSend = this.WhenAnyValue(
            x => x.IsBusy,
            x => x.UserInput,
            (busy, input) => !busy && !string.IsNullOrWhiteSpace(input));

        SendMessageCommand = ReactiveCommand.CreateFromTask(
            SendMessageAsync,
            canSend);
    }

    private void OnMessageAdded(object? sender, Message message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (message is UserLldbCommandMessage) return;
            
            UiMessages.Add(new UiMessage(message));
            
        });
    }

    public string UserInput
    {
        get => _userInput;
        set => this.RaiseAndSetIfChanged(ref _userInput, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userText = UserInput.Trim();
        UserInput = string.Empty; 
        IsBusy = true;

        try
        {
            _agentService.AddUserMessage(userText);
            await _agentService.ProcessLastUserMessageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}

