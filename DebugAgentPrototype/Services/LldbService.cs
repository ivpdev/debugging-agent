using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DebugAgentPrototype.Models;

namespace DebugAgentPrototype.Services;

public class LldbService
{
    private Process? _lldbProcess;
    private StreamWriter? _lldbInput;
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _outputLock = new();
    private bool _isRunning;

    public event EventHandler<string>? OutputReceived;

    public bool IsRunning => _isRunning;

    public async Task StartAsync(IReadOnlyList<Breakpoint> breakpoints, CancellationToken ct)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("LLDB session is already running");
        }

        var inspectedFilePath = SourceCodeService.GetInspectedFilePath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "lldb",
            Arguments = inspectedFilePath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
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
        _lldbInput = _lldbProcess.StandardInput;
        _lldbProcess.BeginOutputReadLine();
        _lldbProcess.BeginErrorReadLine();
        _isRunning = true;

        if (breakpoints.Count > 0)
        {
            await Task.Delay(500, ct);
            foreach (var bp in breakpoints)
            {
                await SendCommandAsync($"br set --file game.c --line {bp.Line}", ct);
            }
        }

        // Start the program
        await Task.Delay(500, ct);
        await SendCommandAsync("run", ct);
    }

    public async Task SendCommandAsync(string command, CancellationToken ct)
    {
        if (!_isRunning || _lldbInput == null)
        {
            throw new InvalidOperationException("LLDB session is not running");
        }

        Console.WriteLine($"[LLDB] Sending command: {command}");
        await _lldbInput.WriteLineAsync(command);
        await _lldbInput.FlushAsync();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        try
        {
            _lldbInput?.Close();
            _lldbProcess?.Kill();
            _lldbProcess?.WaitForExit(1000);
        }
        catch
        {
            // Ignore errors during cleanup
        }
        finally
        {
            _lldbProcess?.Dispose();
            _lldbProcess = null;
            _lldbInput = null;
            _isRunning = false;
        }
    }

    private void OnOutputReceived(string output)
    {
        lock (_outputLock)
        {
            _outputBuffer.AppendLine(output);
        }

        OutputReceived?.Invoke(this, output);
    }

    public string GetOutput()
    {
        lock (_outputLock)
        {
            return _outputBuffer.ToString();
        }
    }
}

