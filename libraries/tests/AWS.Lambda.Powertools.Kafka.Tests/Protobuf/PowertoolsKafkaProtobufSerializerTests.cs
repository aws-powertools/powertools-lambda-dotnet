using TestKafka;

namespace AWS.Lambda.Powertools.Kafka.Tests.Protobuf;

public class PowertoolsKafkaProtobufSerializerTests
{
    [Fact]
    public void DeserializeProtobufFromBase64()
    {
        // Base64 encoded Protobuf data
        string base64EncodedProto = "COkHEgZMYXB0b3AZUrgehes/j0A=";

        // Decode base64 to bytes
        byte[] protoBytes = Convert.FromBase64String(base64EncodedProto);

        // Deserialize to ProtobufProduct
        var product = ProtobufProduct.Parser.ParseFrom(protoBytes);

        // Verify values
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(1001, product.Id);
        Assert.Equal(999.99, product.Price);
    }
}