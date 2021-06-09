using System;
using System.Collections.Generic;
using System.Text.Json;
using Amazon.LambdaPowertools.Metrics;
using Amazon.LambdaPowertools.Metrics.Model;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        private readonly IMetricsLogger _metricsLogger;

        public ValuesController(IMetricsLogger metricsLogger)
        {
            _metricsLogger = metricsLogger;
        }

        // GET api/values
        [HttpGet]
        public string Get()
        {
            try
            {
                List<int> randomList = new List<int>();
                var rnd = new Random();

                for (int i = 0; i < 50; i++)
                {
                    var value = rnd.Next(0, 10);
                    randomList.Add(value);

                    _metricsLogger.AddMetric($"Metric-{value}", value, Unit.COUNT);
                }

                return JsonSerializer.Serialize(randomList);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
