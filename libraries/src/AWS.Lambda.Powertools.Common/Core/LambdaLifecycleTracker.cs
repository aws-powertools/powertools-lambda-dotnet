using System;
using System.Threading;

namespace AWS.Lambda.Powertools.Common.Core;

/// <summary>
/// Tracks Lambda lifecycle state including cold starts
/// </summary>
internal static class LambdaLifecycleTracker
{
    // Static flag that's true only for the first Lambda container initialization
    private static bool _isFirstContainer = true;
        
    // Store the cold start state for the current invocation
    private static readonly AsyncLocal<bool?> CurrentInvocationColdStart = new AsyncLocal<bool?>();

    private static string _lambdaInitType;
    private static string LambdaInitType => _lambdaInitType ?? Environment.GetEnvironmentVariable(Constants.AWSInitializationTypeEnv);
    
    /// <summary>
    /// Returns true if the current Lambda invocation is a cold start
    /// </summary>
    public static bool IsColdStart
    {
        get
        {
            if(LambdaInitType == "provisioned-concurrency")
            {
                // If the Lambda is provisioned concurrency, it is not a cold start
                return false;
            }
            
            // Initialize the cold start state for this invocation if not already set
            if (!CurrentInvocationColdStart.Value.HasValue)
            {
                // Capture the container's cold start state for this entire invocation
                CurrentInvocationColdStart.Value = _isFirstContainer;
                    
                // After detecting the first invocation, mark future ones as warm
                if (_isFirstContainer)
                {
                    _isFirstContainer = false;
                }
            }
                
            // Return the cold start state for this invocation (cannot change during the invocation)
            return CurrentInvocationColdStart.Value ?? false;
        }
    }
    
    

    /// <summary>
    /// Resets the cold start state for testing
    /// </summary>
    /// <param name="resetContainer">Whether to reset the container state (defaults to true)</param>
    internal static void Reset(bool resetContainer = true)
    {
        if (resetContainer)
        {
            _isFirstContainer = true;
        }
        CurrentInvocationColdStart.Value = null;
        _lambdaInitType = null;
    }
}