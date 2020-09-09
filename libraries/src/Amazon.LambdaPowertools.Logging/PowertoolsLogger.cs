// using System;
// using Microsoft.Extensions.Logging;
//
// namespace Amazon.LambdaPowertools.Logging
// {
//     public class PowertoolsLogger : ILogger
//     {
//         // private readonly LoggerOptions _loggerOptions;
//         // public PowertoolsLogger(LoggerOptions loggerOptions)
//         // {
//         //     _loggerOptions = loggerOptions;
//         // }
//         public PowertoolsLogger()
//         {
//         }
//
//         public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//         {
//             Console.WriteLine(formatter);
//         }
//
//         public bool IsEnabled(LogLevel logLevel)
//         {
//             throw new NotImplementedException();
//         }
//
//         public IDisposable BeginScope<TState>(TState state)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }