using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AWS.Lambda.PowerTools.Metrics.Serializer
{
    public class UnixMillisecondDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            long ms;
            if(value is DateTime dateTime)
            {
                ms = (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
            }
            else
            {
                throw new JsonException("Expected Date object value.");
            }

            if(ms < 0)
            {
                throw new JsonException("Invalid date");
            }

            writer.WriteNumberValue(ms);
        }
    }
}
