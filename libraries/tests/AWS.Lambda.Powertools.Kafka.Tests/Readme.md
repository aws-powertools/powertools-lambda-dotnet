# Avro

```bash
dotnet tool install --global Apache.Avro.Tools

cd tests/AWS.Lambda.Powertools.Kafka.Tests/Avro/
avrogen -s AvroProduct.avsc ./
```

```xml
<Target Name="GenerateAvroClasses" BeforeTargets="CoreCompile">
  <Exec Command="avrogen -s $(ProjectDir)AvroProduct.avsc $(ProjectDir)Generated" />
  <ItemGroup>
    <Compile Include="$(ProjectDir)Generated/**/*.cs" />
  </ItemGroup>
</Target>
```
