using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using AWS.Lambda.PowerTools.Metrics.Serializer;

namespace AWS.Lambda.PowerTools.Metrics
{       
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MetricUnit
    {        
        [EnumMember(Value = "None")]
        NONE,
        [EnumMember(Value = "Seconds")]
        SECONDS,
        [EnumMember(Value = "Microseconds")]
        MICROSECONDS,
        [EnumMember(Value = "Milliseconds")]
        MILLISECONDS,
        [EnumMember(Value = "Bytes")]
        BYTES,
        [EnumMember(Value = "Kilobytes")]
        KILOBYTES,
        [EnumMember(Value = "Megabytes")]
        MEGABYTES,
        [EnumMember(Value = "Gigabytes")]
        GIGABYTES,
        [EnumMember(Value = "Terabytes")]
        TERABYTES,
        [EnumMember(Value = "Bits")]
        BITS,
        [EnumMember(Value = "Kilobits")]
        KILOBITS,
        [EnumMember(Value = "Megabits")]
        MEGABITS,
        [EnumMember(Value = "Gigabits")]
        GIGABITS,
        [EnumMember(Value = "Terabits")]
        TERABITS,
        [EnumMember(Value = "Percent")]
        PERCENT,
        [EnumMember(Value = "Count")]
        COUNT,
        [EnumMember(Value = "Bytes/Second")]
        BYTES_PER_SECOND,
        [EnumMember(Value = "Kilobytes/Second")]
        KILOBYTES_PER_SECOND,
        [EnumMember(Value = "Megabytes/Second")]
        MEGABYTES_PER_SECOND,
        [EnumMember(Value = "Gigabytes/Second")]
        GIGABYTES_PER_SECOND,
        [EnumMember(Value = "Terabytes/Second")]
        TERABYTES_PER_SECOND,
        [EnumMember(Value = "Bits/Second")]
        BITS_PER_SECOND,
        [EnumMember(Value = "Kilobits/Second")]
        KILOBITS_PER_SECOND,
        [EnumMember(Value = "Megabits/Second")]
        MEGABITS_PER_SECOND,
        [EnumMember(Value = "Gigabits/Second")]
        GIGABITS_PER_SECOND,
        [EnumMember(Value = "Terabits/Second")]
        TERABITS_PER_SECOND,
        [EnumMember(Value = "Count/Second")]
        COUNT_PER_SECOND
    }
}