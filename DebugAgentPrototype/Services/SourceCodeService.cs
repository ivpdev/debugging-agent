using System;
using System.IO;
using System.Linq;

namespace DebugAgentPrototype.Services;

public class SourceCodeService
{
    private static readonly string InspectedProgramBasePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "inspected_program"));
    
    public static string GetSourceCode(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public static string GetInspectedFilePath()
    {
        var gamePath = Path.Combine(InspectedProgramBasePath, "game.c");

        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException($"Game executable not found at: {gamePath}");
        }

        return gamePath;
    }

    public static string GetBinaryPath()
    {
        var gamePath = Path.Combine(InspectedProgramBasePath, "game");

        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException($"Game binary not found at: {gamePath}");
        }

        return gamePath;
    }

    public static string GetInpectedFileName()
    {
        return Path.GetFileName(GetInspectedFilePath());
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