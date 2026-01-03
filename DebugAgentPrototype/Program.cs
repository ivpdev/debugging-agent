using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using DotNetEnv;

namespace DebugAgentPrototype;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Env.Load();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}

