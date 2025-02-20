FROM mcr.microsoft.com/dotnet/sdk@sha256:c8fdd06e430de9f4ddd066b475ea350d771f341b77dd5ff4c2fafa748e3f2ef2 AS build-image

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
RUN if [ "$SAM_BUILD_MODE" = "debug" ]; then cp -r /build/bin/Debug/net6.0/publish/* /build/build_artifacts; else cp -r /build/bin/Release/net6.0/publish/* /build/build_artifacts; fi

FROM public.ecr.aws/lambda/dotnet:6

COPY --from=build-image /build/build_artifacts/ /var/task/
# Command can be overwritten by providing a different command in the template directly.
CMD ["HelloWorld::HelloWorld.Function::FunctionHandler"]