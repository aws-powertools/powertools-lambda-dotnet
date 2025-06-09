# Powertools for AWS Lambda - Kafka examples


## Already added to the project

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

## Here are some steps to follow to get started from the command line:


Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

## Edit the aws-lambda-tools-defaults.json file

Update the role to use in the `aws-lambda-tools-defaults.json` file. This file is used by the `dotnet lambda deploy-function` command to deploy the Lambda function.

```
    code aws-lambda-tools-defaults.json
```

Deploy function to AWS Lambda
```
    dotnet lambda deploy-function
```

## Infra

Make sure the Lambda function adds permissions for bedrock to invoke it. You can do this by running the following command:

```bash 
aws lambda add-permission --function-name <your-function-name> --principal bedrock.amazonaws.com --statement-id <unique-statement-id> --action lambda:InvokeFunction
```

## Invoke the function

Use the provided test event to invoke the function. You can do this with the AWS CLI or the dotnet CLI.

```bash
dotnet lambda invoke-function <your-function-name> --payload file://Avro/kafka-event.json
```
