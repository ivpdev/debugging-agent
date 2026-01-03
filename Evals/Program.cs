using System;
using System.IO;
using System.Threading.Tasks;
using DebugAgentPrototype.Services;
using DotNetEnv;

namespace Evals;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Env.Load();
            var evalService = new EvalService();
            var evalName = GetEvalNameFromArguments(args);
            
            if (evalName != null)
            {
                await evalService.RunEvalByNameAsync(evalName);
            }
            else
            {
                await evalService.RunAllEvalsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static string? GetEvalNameFromArguments(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0].Trim();
        } else {
            return null;
        }
    }
}


