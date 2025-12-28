using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Services;
using DotNetEnv;

namespace Evals;

class Program
{
    static async Task Main(string[] args)
    {
        LoadEnvironmentFile();
        
        var openRouterService = new OpenRouterService();
        var evalService = new EvalService(openRouterService);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await evalService.RunAllEvalsAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nEvaluation cancelled by user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void LoadEnvironmentFile()
    {
        var possiblePaths = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "DebugAgentPrototype", ".env")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "DebugAgentPrototype", ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env")
        };

        bool loaded = false;
        foreach (var envPath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(envPath);
            if (File.Exists(fullPath))
            {
                Console.WriteLine($"Loading .env from: {fullPath}");
                Env.Load(fullPath);
                loaded = true;
                break;
            }
        }

        if (!loaded)
        {
            try
            {
                Env.Load();
                loaded = true;
            }
            catch
            {
            }
        }

        if (!loaded)
        {
            Console.WriteLine("Warning: .env file not found. Make sure OPENROUTER_API_KEY is set.");
        }
    }
}

