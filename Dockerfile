FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build-image

ARG FUNCTION_DIR="/build"
ARG SAM_BUILD_MODE="run"
ENV PATH="/root/.dotnet/tools:${PATH}"

RUN apt-get update && apt-get -y install zip

RUN mkdir $FUNCTION_DIR
WORKDIR $FUNCTION_DIR
COPY examples/SimpleLambda/src/HelloWorld/Function.cs examples/SimpleLambda/src/HelloWorld/HelloWorld.csproj examples/SimpleLambda/src/HelloWorld/aws-lambda-tools-defaults.json $FUNCTION_DIR/examples/SimpleLambda/src/HelloWorld/
COPY libraries/src/ $FUNCTION_DIR/libraries/src/
RUN dotnet tool install -g Amazon.Lambda.Tools

# Build and Copy artifacts depending on build mode.
RUN mkdir -p build_artifacts
WORKDIR $FUNCTION_DIR/examples/SimpleLambda/src/HelloWorld/
RUN if [ "$SAM_BUILD_MODE" = "debug" ]; then dotnet lambda package --configuration Debug; else dotnet lambda package --configuration Release; fi
RUN if [ "$SAM_BUILD_MODE" = "debug" ]; then cp -r /bin/Debug/net6.0/publish/* /build/build_artifacts; else cp -r bin/Release/net6.0/publish/* /build/build_artifacts; fi

FROM public.ecr.aws/lambda/dotnet:6

COPY --from=build-image /build/build_artifacts/ /var/task/
# Command can be overwritten by providing a different command in the template directly.
CMD ["HelloWorld::HelloWorld.Function::FunctionHandler"]
