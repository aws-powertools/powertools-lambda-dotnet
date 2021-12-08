using System;
using Newtonsoft.Json;

namespace AWS.Lambda.PowerTools.Metrics.Serializer
{
    public class UnixMillisecondDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            var ms = (long)(value.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
            
            if(ms < 0)
            {
                throw new JsonException("Invalid date");
            }

            writer.WriteValue(ms);
        }

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
