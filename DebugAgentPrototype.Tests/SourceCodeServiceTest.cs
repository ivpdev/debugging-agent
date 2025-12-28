using System;
using System.IO;
using DebugAgentPrototype.Services;
using Xunit;

namespace DebugAgentPrototype.Tests;

public class SourceCodeServiceTest
{
    [Fact]
    public void TestGetSourceCode()
    {
        var testFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "test_program", "game.c");
        if (!Path.IsPathRooted(testFilePath))
        {
            testFilePath = Path.GetFullPath(testFilePath);
        }

        if (!File.Exists(testFilePath))
        {
            Console.WriteLine($"Test file not found at: {testFilePath}");
            Assert.True(false, $"Test file not found at: {testFilePath}");
            return;
        }

        var result = SourceCodeService.GetSourceCode(testFilePath);
        
        Console.WriteLine("=== GetSourceCode Result ===");
        Console.WriteLine(result);
        Console.WriteLine("=== End of Result ===");
        
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void TestGetInspectedFileContent()
    {
        var result = SourceCodeService.GetInspectedFileContent();
        
        Console.WriteLine("=== GetInspectedFileContent Result ===");
        Console.WriteLine($"Content length: {result.Length} characters");
        Console.WriteLine($"First 200 characters: {result.Substring(0, Math.Min(200, result.Length))}");
        Console.WriteLine("=== End of Result ===");
        
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}

