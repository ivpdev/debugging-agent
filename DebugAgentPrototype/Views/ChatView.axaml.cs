using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using DebugAgentPrototype.ViewModels;

namespace DebugAgentPrototype.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }
    
    private void OnUserInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ChatViewModel vm)
        {
            vm.SendMessageCommand.Execute(Unit.Default);
            e.Handled = true;
        }
    }
}

