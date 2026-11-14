// Compile-only stand-ins for the types the Kafka snippets use.
// In a real project these classes are generated from Avro (.avsc) or Protobuf (.proto) schemas,
// or are part of the user's domain. This file is not included in the docs.

using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Docs.KafkaSnippets
{
    public class CustomerProfile
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public long Age { get; set; }
        public string AccountStatus { get; set; }
    }

    public class CustomerKey
    {
        public string Id { get; set; }
    }

    public class Payment
    {
        public string Id { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace Docs.KafkaSnippets.TestKafka
{
    // Stand-in for the class protoc generates from:
    // message ProtobufProduct { int32 id = 1; string name = 2; double price = 3; }
    public sealed class ProtobufProduct : IMessage<ProtobufProduct>
    {
        public static MessageParser<ProtobufProduct> Parser { get; } = new(() => new ProtobufProduct());

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }

        public MessageDescriptor Descriptor => throw new NotSupportedException();

        public void MergeFrom(ProtobufProduct message)
        {
            Id = message.Id;
            Name = message.Name;
            Price = message.Price;
        }

        public void MergeFrom(CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    case 8: Id = input.ReadInt32(); break;
                    case 18: Name = input.ReadString(); break;
                    case 25: Price = input.ReadDouble(); break;
                    default: input.SkipLastField(); break;
                }
            }
        }

        public void WriteTo(CodedOutputStream output)
        {
            if (Id != 0) { output.WriteRawTag(8); output.WriteInt32(Id); }
            if (Name.Length != 0) { output.WriteRawTag(18); output.WriteString(Name); }
            if (Price != 0D) { output.WriteRawTag(25); output.WriteDouble(Price); }
        }

        public int CalculateSize()
        {
            var size = 0;
            if (Id != 0) size += 1 + CodedOutputStream.ComputeInt32Size(Id);
            if (Name.Length != 0) size += 1 + CodedOutputStream.ComputeStringSize(Name);
            if (Price != 0D) size += 1 + 8;
            return size;
        }

        public ProtobufProduct Clone() => new() { Id = Id, Name = Name, Price = Price };

        public bool Equals(ProtobufProduct other) =>
            other != null && Id == other.Id && Name == other.Name && Price.Equals(other.Price);
    }
}
