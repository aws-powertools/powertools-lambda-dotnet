

using System;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

internal class BatchProcessingAspectHandler<TEvent, TRecord> : IBatchProcessingAspectHandler
{
    private readonly IBatchProcessor<TEvent, TRecord> _batchProcessor;
    private readonly IRecordHandler<TRecord> _recordHandler;
    private readonly ProcessingOptions _processingOptions;

    public BatchProcessingAspectHandler(IBatchProcessor<TEvent, TRecord> batchProcessor, IRecordHandler<TRecord> recordHandler, ProcessingOptions processingOptions)
    {
        _batchProcessor = batchProcessor;
        _recordHandler = recordHandler;
        _processingOptions = processingOptions;
    }

    public async Task HandleAsync(object[] args)
    {
        // Try get event from args
        if (args?.FirstOrDefault() is not TEvent @event)
        {
            throw new InvalidOperationException($"The first function handler parameter must be of type: '{typeof(TEvent).Namespace}'.");
        }

        // Run batch processor
        await _batchProcessor.ProcessAsync(@event, _recordHandler, _processingOptions);
    }
}