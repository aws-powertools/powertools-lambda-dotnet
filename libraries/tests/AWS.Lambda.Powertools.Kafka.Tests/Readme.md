# Avro

```bash
dotnet tool install --global Apache.Avro.Tools

cd tests/AWS.Lambda.Powertools.Kafka.Tests/Avro/
avrogen -s AvroProduct.avsc ./
```

```xml

<Target Name="GenerateAvroClasses" BeforeTargets="CoreCompile">
    <Exec Command="avrogen -s $(ProjectDir)AvroProduct.avsc $(ProjectDir)Generated"/>
    <ItemGroup>
        <Compile Include="$(ProjectDir)Generated/**/*.cs"/>
    </ItemGroup>
</Target>
```

# Protobuf

```xml

<None Remove="Protobuf\Product.proto"/>
<Protobuf Include="Protobuf\Product.proto" GrpcServices="Client">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Protobuf>

<PackageReference Include="Grpc.Tools">

```