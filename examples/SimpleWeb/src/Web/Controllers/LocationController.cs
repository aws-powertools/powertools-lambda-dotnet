using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AWS.Lambda.PowerTools.Metrics;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IMetrics Metrics;
        private static HttpClient _client;

        public LocationController(IMetrics metricsLogger)
        {
            Metrics = metricsLogger;
            _client = new HttpClient();
        }

        // GET api/location
        [HttpGet]
        public async Task<string> Get()
        {
            try
            {
                Metrics.AddMetric("GetLocationCount", 1, MetricUnit.COUNT);

                var watch = System.Diagnostics.Stopwatch.StartNew();
                var ip = await GetCallingIP();
                watch.Stop();

                Metrics.AddMetric("GetIPExecutionTime", watch.ElapsedMilliseconds, MetricUnit.MILLISECONDS);

                watch.Restart();
                var locationInfo = await GetIPLocation(ip);
                watch.Stop();


                Metrics.AddMetric("GetIPLocationInfoExecutionTime", watch.ElapsedMilliseconds, MetricUnit.MILLISECONDS);

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

            var msg = await _client.GetStringAsync("https://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext: false);

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
