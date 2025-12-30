using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DebugAgentPrototype.ViewModels;
using DebugAgentPrototype.Views;

namespace DebugAgentPrototype;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            
            await mainViewModel.InitializeLldbAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

