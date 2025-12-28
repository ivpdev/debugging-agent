using System;
using System.IO;
using System.Linq;

namespace DebugAgentPrototype.Services;

public class SourceCodeService
{
    public static string GetSourceCode(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public static string GetInspectedFilePath()
    {
        var gamePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "test_program", "game.c");
        if (!Path.IsPathRooted(gamePath))
        {
            gamePath = Path.GetFullPath(gamePath);
        }

        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException($"Game executable not found at: {gamePath}");
        }

        return gamePath;
    }

    public static string GetBinaryPath()
    {
        var gamePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "test_program", "game");
        if (!Path.IsPathRooted(gamePath))
        {
            gamePath = Path.GetFullPath(gamePath);
        }

        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException($"Game binary not found at: {gamePath}");
        }

        return gamePath;
    }

    private static string PrefixLineNumbers(string content)
    {
        return string.Join("\n", content.Split('\n').Select((line, index) => $"{index + 1}: {line}"));
    }

    public static string GetInspectedFileContent()
    {
        var inspectedFilePath = GetInspectedFilePath();
        return PrefixLineNumbers(File.ReadAllText(inspectedFilePath));
    }
}