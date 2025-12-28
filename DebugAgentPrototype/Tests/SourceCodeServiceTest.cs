using System;
using System.IO;
using DebugAgentPrototype.Services;

namespace DebugAgentPrototype.Tests;

public class SourceCodeServiceTest
{
    public static void TestGetSourceCode()
    {
        var testFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "test_program", "game.c");
        if (!Path.IsPathRooted(testFilePath))
        {
            testFilePath = Path.GetFullPath(testFilePath);
        }

        if (!File.Exists(testFilePath))
        {
            Console.WriteLine($"Test file not found at: {testFilePath}");
            return;
        }

        var result = SourceCodeService.GetSourceCode(testFilePath);
        Console.WriteLine("=== GetSourceCode Result ===");
        Console.WriteLine(result);
        Console.WriteLine("=== End of Result ===");
    }
}

