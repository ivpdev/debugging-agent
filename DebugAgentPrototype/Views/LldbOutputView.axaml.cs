using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using DebugAgentPrototype.ViewModels;

namespace DebugAgentPrototype.Views;

public partial class LldbOutputView : UserControl
{
    public LldbOutputView()
    {
        InitializeComponent();
    }

    private void OnLldbInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LldbOutputViewModel vm)
        {
            vm.SendLldbCommand.Execute(Unit.Default);
            e.Handled = true;
        }
    }
}

