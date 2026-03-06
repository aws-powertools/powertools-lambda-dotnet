using System;
using System.IO;

namespace AWS.Lambda.Powertools.Common;

/// <inheritdoc />
public class ConsoleWrapper : IConsoleWrapper
{
    private static bool _override;
    private static TextWriter _testOutputStream;
    private static bool _inTestMode = false;
    private static StreamWriter _stdoutWriter;
    private static StreamWriter _stderrWriter;
    private static readonly object _lock = new object();

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            EnsureConsoleOutput();
            Console.WriteLine(message);
        }
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            EnsureConsoleOutput();
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        if (_inTestMode && _testOutputStream != null)
        {
            _testOutputStream.WriteLine(message);
        }
        else
        {
            EnsureStderrOutput();
            Console.Error.WriteLine(message);
        }
    }

    /// <summary>
    ///     Set the ConsoleWrapper to use a different TextWriter
    ///     This is useful for unit tests where you want to capture the output
    /// </summary>
    public static void SetOut(TextWriter consoleOut)
    {
        _testOutputStream = consoleOut;
        _inTestMode = true;
        _override = true;
        Console.SetOut(consoleOut);
    }
    
    private static void EnsureConsoleOutput()
    {
        // Check if we need to override console output for Lambda environment
        if (ShouldOverrideConsole())
        {
            OverrideLambdaLogger();
        }
    }

    private static void EnsureStderrOutput()
    {
        if (_inTestMode) return;
        
        var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
        if (!isLambda) return;
        
        lock (_lock)
        {
            if (_stderrWriter != null) return;
            
            try
            {
                _stderrWriter = new StreamWriter(Console.OpenStandardError())
                {
                    AutoFlush = true
                };
                Console.SetError(_stderrWriter);
            }
            catch (Exception)
            {
                // Degraded functionality is better than crash
            }
        }
    }

    private static bool ShouldOverrideConsole()
    {
        // Don't override if we're in test mode
        if (_inTestMode) return false;

        // Always override in Lambda environment to prevent Lambda's log wrapping
        var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

        return isLambda && (!_override || HasLambdaReInterceptedConsole());
    }

    internal static bool HasLambdaReInterceptedConsole()
    {
        return HasLambdaReInterceptedConsole(() => Console.Out);
    }
    
    internal static bool HasLambdaReInterceptedConsole(Func<TextWriter> consoleOutAccessor)
    {
        // Lambda might re-intercept console between init and handler execution.
        // We need to detect when Lambda replaces our writer with its own,
        // but NOT trigger on the SyncTextWriter wrapper that Console.SetOut
        // always applies around our StreamWriter — that's still ours.
        try
        {
            var currentOut = consoleOutAccessor();
            var typeName = currentOut.GetType().FullName ?? "";
            
            // If it explicitly contains "Lambda", Lambda has re-intercepted
            if (typeName.Contains("Lambda"))
                return true;
            
            // If we have a cached writer, check if Console.Out still wraps it.
            // Console.SetOut wraps in SyncTextWriter, so seeing SyncTextWriter
            // does NOT mean Lambda re-intercepted — it's our own writer wrapped.
            // Only if _stdoutWriter is null (never set) do we need to override.
            lock (_lock)
            {
                return _stdoutWriter == null;
            }
        }
        catch
        {
            return true; // Assume re-interception if we can't determine
        }
    }
    
    internal static void OverrideLambdaLogger()
    {
        OverrideLambdaLogger(() => Console.OpenStandardOutput());
    }
    
    internal static void OverrideLambdaLogger(Func<Stream> standardOutputOpener)
    {
        lock (_lock)
        {
            try
            {
                // Reuse existing writer if we already have one — avoids FD leak
                if (_stdoutWriter != null)
                {
                    // Re-set Console.Out in case Lambda replaced it
                    Console.SetOut(_stdoutWriter);
                    _override = true;
                    return;
                }
                
                // First time: create a single long-lived writer for stdout
                _stdoutWriter = new StreamWriter(standardOutputOpener())
                {
                    AutoFlush = true
                };
                Console.SetOut(_stdoutWriter);
                _override = true;
            }
            catch (Exception)
            {
                // Log the failure but don't throw - degraded functionality is better than crash
                _override = false;
            }
        }
    }
    
    internal static void WriteLine(string logLevel, string message)
    {
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\t{logLevel}\t{message}");
    }

    /// <summary>
    ///     Reset the ConsoleWrapper to its original state
    /// </summary>
    public static void ResetForTest()
    {
        _override = false;
        _inTestMode = false;
        _testOutputStream = null;
        _stdoutWriter = null;
        _stderrWriter = null;
    }
    
    /// <summary>
    ///     Clear the output reset flag
    /// </summary>
    public static void ClearOutputResetFlag()
    {
        // This method is kept for backward compatibility but no longer needed
        // since we removed the _outputResetPerformed flag
    }
}
