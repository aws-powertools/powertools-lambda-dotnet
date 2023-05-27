using Microsoft.AspNetCore.Mvc;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;

namespace LambdaPowertoolsAPI.Controllers;

[Route("api/[controller]")]
public class ValuesController : ControllerBase
{
    // GET api/values
    [HttpGet]
    [Tracing(SegmentName = "Values::Get")]
    public IEnumerable<string> Get()
    {
        Logger.LogInformation("Log entry information only about getting values? Or maybe something more ");

        return new string[] { "value1", "value2" };
    }

    // GET api/values/5
    [HttpGet("{id}")]
    [Tracing(SegmentName = "Values::GetById")]
    public string Get(int id)
    {

        Metrics.AddMetric("SuccessfulRetrieval", 1, MetricUnit.Count);
        return "value";
    }

    // POST api/values
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/values/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/values/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}