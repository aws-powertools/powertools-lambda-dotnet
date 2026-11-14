FROM mcr.microsoft.com/dotnet/sdk@sha256:e1ffd2a92ae84c1291bc1b6887501f8af98e6331e7af6d4c8d37168c5e87a64c AS build-image

ARG FUNCTION_DIR="/build"
ARG SAM_BUILD_MODE="run"
ENV PATH="/root/.dotnet/tools:${PATH}"
RUN apt-get update && apt-get -y install zip

RUN mkdir $FUNCTION_DIR
WORKDIR $FUNCTION_DIR
COPY Function.cs HelloWorld.csproj aws-lambda-tools-defaults.json $FUNCTION_DIR/
RUN dotnet tool install -g Amazon.Lambda.Tools

# Build and Copy artifacts depending on build mode.
RUN mkdir -p build_artifacts
RUN if [ "$SAM_BUILD_MODE" = "debug" ]; then dotnet lambda package --configuration Debug; else dotnet lambda package --configuration Release; fi
RUN if [ "$SAM_BUILD_MODE" = "debug" ]; then cp -r /build/bin/Debug/net10.0/publish/* /build/build_artifacts; else cp -r /build/bin/Release/net10.0/publish/* /build/build_artifacts; fi

FROM public.ecr.aws/lambda/dotnet@sha256:6a4e0d15ca7b7fa6879155f5ceef7231a19ab099756a7e36090db2bf2fde281f

COPY --from=build-image /build/build_artifacts/ /var/task/
# Command can be overwritten by providing a different command in the template directly.
CMD ["HelloWorld::HelloWorld.Function::FunctionHandler"]