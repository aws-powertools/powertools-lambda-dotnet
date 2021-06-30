using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.LambdaPowertools.Metrics;
using Amazon.LambdaPowertools.Metrics.Model;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IMetricsLogger _metricsLogger;
        private static HttpClient _client;

        public LocationController(IMetricsLogger metricsLogger)
        {
            _metricsLogger = metricsLogger;
            _client = new HttpClient();
        }

        // GET api/location
        [HttpGet]
        public async Task<string> Get()
        {
            try
            {
                _metricsLogger.AddMetric("GetLocationCount", 1, MetricsUnit.COUNT);

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var ip = await GetCallingIP();
                watch.Stop();

                _metricsLogger.AddMetric("GetIPExecutionTime", watch.ElapsedMilliseconds, MetricsUnit.MILLISECONDS);

                watch.Restart();
                var locationInfo = await GetIPLocation(ip);
                watch.Stop();


                _metricsLogger.AddMetric("GetIPLocationInfoExecutionTime", watch.ElapsedMilliseconds, MetricsUnit.MILLISECONDS);

                return JsonSerializer.Serialize(locationInfo, typeof(LocationInfo));
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private static async Task<string> GetCallingIP()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var msg = await _client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext: false);

            return msg.Replace("\n", "");
        }

        private static async Task<LocationInfo> GetIPLocation(string IP)
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var result = await _client.GetAsync($"http://ip-api.com/json/{IP}").ConfigureAwait(continueOnCapturedContext: false);
            var content = await result.Content.ReadAsStringAsync();

            return (LocationInfo)JsonSerializer.Deserialize(content, typeof(LocationInfo));
        }
    }
}
