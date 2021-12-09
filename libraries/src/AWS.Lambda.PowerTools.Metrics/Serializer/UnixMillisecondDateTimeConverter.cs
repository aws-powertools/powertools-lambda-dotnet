using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics.Serializer
{
    public class UnixMillisecondDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var ms = (long)(value.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
            
            if(ms < 0)
            {
                throw new JsonException("Invalid date");
            }

            writer.WriteNumberValue(ms);
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
