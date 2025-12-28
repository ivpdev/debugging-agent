using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DebugAgentPrototype.ViewModels;

namespace DebugAgentPrototype.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void OnUserInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            vm.SendMessageCommand.Execute(Unit.Default);
            e.Handled = true;
        }
    }
}

