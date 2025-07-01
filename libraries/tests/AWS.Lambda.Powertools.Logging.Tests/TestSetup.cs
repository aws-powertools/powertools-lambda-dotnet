using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

[assembly: TestCollectionOrderer("AWS.Lambda.Powertools.Logging.Tests.DisplayNameOrderer", "AWS.Lambda.Powertools.Logging.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]


namespace AWS.Lambda.Powertools.Logging.Tests;

public class DisplayNameOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        return testCollections.OrderBy(collection => collection.DisplayName);
    }
}
