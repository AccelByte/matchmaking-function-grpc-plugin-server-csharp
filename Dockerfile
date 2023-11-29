FROM mcr.microsoft.com/dotnet/sdk:6.0.417 as builder
ARG PROJECT_PATH=src/AccelByte.PluginArch.Demo.Server
WORKDIR /build
COPY $PROJECT_PATH/ .
RUN dotnet publish -c Release -o output

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:6.0.417
WORKDIR /app
COPY --from=builder /build/output/ .
# Plugin arch gRPC server port
EXPOSE 6565
# Prometheus /metrics web server port
EXPOSE 8080
ENTRYPOINT ["/app/AccelByte.PluginArch.Demo.Server"]
