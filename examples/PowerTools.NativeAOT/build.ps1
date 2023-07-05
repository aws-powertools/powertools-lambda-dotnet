dotnet publish -c Release -r linux-x64 ../../libraries/src/AWS.Lambda.Powertools.Logging/AWS.Lambda.Powertools.Logging.csproj -o ./src/HelloWorld/Assemblies
dotnet publish -c Release -r linux-x64 ../../libraries/src/AWS.Lambda.Powertools.Metrics/AWS.Lambda.Powertools.Metrics.csproj -o ./src/HelloWorld/Assemblies
dotnet publish -c Release -r linux-x64 ../../libraries/src/AWS.Lambda.Powertools.Tracing/AWS.Lambda.Powertools.Tracing.csproj -o ./src/HelloWorld/Assemblies
sam build