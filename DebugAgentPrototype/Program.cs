using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using DotNetEnv;

namespace DebugAgentPrototype;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        LoadEnvironmentFile();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void LoadEnvironmentFile()
    {
        // Load .env file - try multiple locations
        var possiblePaths = new[]
        {
            // Project root (4 levels up from bin/Debug/net9.0/)
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
            // Solution root (5 levels up)
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env")),
            // Current working directory
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            // Executable directory
            Path.Combine(AppContext.BaseDirectory, ".env")
        };

        bool loaded = false;
        foreach (var envPath in possiblePaths)
        {
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                loaded = true;
                break;
            }
        }

        if (!loaded)
        {
            // Try loading without path (DotNetEnv will search current directory)
            try
            {
                Env.Load();
                loaded = true;
            }
            catch
            {
                // Ignore if .env not found
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}

