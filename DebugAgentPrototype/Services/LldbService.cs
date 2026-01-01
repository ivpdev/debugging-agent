using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class LldbService(AppState appState)
{
    private Process? _lldbProcess;
    private StreamWriter? _lldbInput;
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _outputLock = new object();

    public event EventHandler<string>? OutputReceived;

    //TODO remove
    public bool IsRunning => _lldbProcess != null && _lldbInput != null;

    public async Task InitializeAsync()
    {
        if (_lldbProcess != null)
        {
            throw new InvalidOperationException("LLDB session is already running");
        }

        var binaryPath = SourceCodeService.GetBinaryPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "lldb",
            Arguments = binaryPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        
        _lldbProcess = new Process { StartInfo = startInfo };

        _lldbProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                OnOutputReceived(e.Data);
            }
        };

        _lldbProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                OnOutputReceived(e.Data);
            }
        };

        _lldbProcess.Start();

        _lldbInput = new StreamWriter(_lldbProcess.StandardInput.BaseStream, new UTF8Encoding(false));
        _lldbProcess.BeginOutputReadLine();
        _lldbProcess.BeginErrorReadLine();

        await Task.Delay(500);
    }

    public async Task SendCommandAsync(string command)
    {
        Console.WriteLine($"[LLDB] Sending command: {command}");
        await _lldbInput?.WriteLineAsync(command)!;
        await _lldbInput.FlushAsync();
    }

    private void OnOutputReceived(string output)
    {
        lock (_outputLock)
        {
            _outputBuffer.AppendLine(output);
            appState.LldbOutput = _outputBuffer.ToString();
        }

        OutputReceived?.Invoke(this, output);
    }
}

