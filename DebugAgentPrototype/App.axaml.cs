using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DebugAgentPrototype.Models;
using DebugAgentPrototype.Services;
using DebugAgentPrototype.ViewModels;
using DebugAgentPrototype.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DebugAgentPrototype;

public partial class App : Application
{
    private static ServiceProvider? _serviceProvider;

    public static ServiceProvider ServiceProvider => _serviceProvider 
        ?? throw new InvalidOperationException("Service provider has not been initialized");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            
            await mainViewModel.InitializeLldbAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<AppState>();
        services.AddSingleton<LldbService>();
        services.AddSingleton<OpenRouterService>();
        services.AddSingleton<ToolsService>();
        services.AddSingleton<AgentService>();
        services.AddSingleton<LldbOutputViewModel>();
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }
}

