using System;
using System.IO;

namespace AWS.Lambda.Powertools.Common;

/// <inheritdoc />
public class ConsoleWrapper : IConsoleWrapper
{
    private static bool _override;
    private static TextWriter _testOutputStream;
    private static bool _inTestMode = false;

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
            if (!_override)
            {
                var errorOutput = new StreamWriter(Console.OpenStandardError());
                errorOutput.AutoFlush = true;
                Console.SetError(errorOutput);
            }
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

    private static bool ShouldOverrideConsole()
    {
        // Don't override if we're in test mode
        if (_inTestMode) return false;

        // Always override in Lambda environment to prevent Lambda's log wrapping
        var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

        return isLambda && (!_override || HasLambdaReInterceptedConsole());
    }

    private static bool HasLambdaReInterceptedConsole()
    {
        // Lambda might re-intercept console between init and handler execution
        try
        {
            var currentOut = Console.Out;
            // Check if current output stream looks like it might be Lambda's wrapper
            var typeName = currentOut.GetType().FullName ?? "";
            return typeName.Contains("Lambda") || typeName == "System.IO.TextWriter+SyncTextWriter";
        }
        catch
        {
            return true; // Assume re-interception if we can't determine
        }
    }
    
    private static void OverrideLambdaLogger()
    {
        try
        {
            // Force override of LambdaLogger
            var standardOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(standardOutput);
            _override = true;
        }
        catch (Exception)
        {
            // Log the failure but don't throw - degraded functionality is better than crash
            _override = false;
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