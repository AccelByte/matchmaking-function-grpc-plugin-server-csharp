# matchmaking-function-grpc-plugin-server-csharp

> :warning: **If you are new to AccelByte Cloud Service Customization gRPC Plugin Architecture**: Start reading from `OVERVIEW.md` in `grpc-plugin-dependencies` repository to get the full context.

Justice service customization using gRPC plugin architecture - Server (C#).

## Prerequisites

1. Windows 10 WSL2 or Linux Ubuntu 20.04 with the following tools installed.

    a. bash

    b. docker

    c. docker-compose

    d. make

    e. .net 6 sdk

2. AccelByte Cloud demo environment.

    a. Base URL: https://demo.accelbyte.io.

    b. [Create a Game Namespace](https://docs.accelbyte.io/esg/uam/namespaces.html#tutorials) if you don't have one yet. Keep the `Namespace ID`.

    c. [Create an OAuth Client](https://docs.accelbyte.io/guides/access/iam-client.html) with confidential client type and give it `read` permission to resource `NAMESPACE:{namespace}:MMV2GRPCSERVICE`. Keep the `Client ID` and `Client Secret`.

## Setup

Create `src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json` and fill in the required configuration.

```json
{
  "DirectLogToLoki": false,
  "EnableAuthorization": false,
  "RevocationListRefreshPeriod": 60,
  "AccelByte": {
    "BaseUrl": "https://demo.accelbyte.io",     // Base URL
    "ClientId": "xxxxxxxxxx",                   // Client ID       
    "ClientSecret": "xxxxxxxxxx",               // Client Secret
    "AppName": "MMV2GRPCSERVICE",
    "TraceIdVersion": "1",
    "Namespace": "xxxxxxxxxx",                  // Namespace ID
    "EnableTraceId": true,
    "EnableUserAgentInfo": true,
    "ResourceName": "MMV2GRPCSERVICE"
  }
}
```

> :exclamation: **For the server and client**: Use the same Base URL, Client ID, Client Secret, and Namespace ID.

## Building

To build the application, use the following command.

```
make build
```

To build and create a docker image of the application, use the following command.

```
make image
```

For more details about the command, see [Makefile](Makefile).

## Running

To run the docker image of the application which has been created beforehand, use the following command.

```
docker-compose up
```

OR

To build, create a docker image, and run the application in one go, use the following command.

```
docker-compose up --build
```

## Advanced

### Building Multi-Arch Docker Image

To create a multi-arch docker image of the project, use the following command.

```
make imagex
```

For more details about the command, see [Makefile](Makefile).

### appsettings.*.json

|Key|Description|Default|
|-|-|-|
|Kestrel.Endpoints.Http.Url|Prometheus scrapper endpoint|http://0.0.0.0:8080|
|Kestrel.Endpoints.Grpc.Url|Grpc service endpoint|http://0.0.0.0:6565|
|DirectLogToLoki|Enable sending log directly to Loki|false|
|LokiUrl|Loki URL for sending log (can be overridden by env var)|http://localhost:3100|
|EnableAuthorization|Enable access token validation|true|
|RevocationListRefreshPeriod|Interval value to refresh token revocation list cache (in seconds)|600|
|AccelByte.BaseUrl|AccelByte Cloud url. [Here](https://github.com/AccelByte/accelbyte-csharp-sdk/blob/main/README.md#usage) for more information||
|AccelByte.ClientId|OAuth client id. [Here](https://github.com/AccelByte/accelbyte-csharp-sdk/blob/main/README.md#usage) for more information||
|AccelByte.ClientSecret|OAuth client secret. [Here](https://github.com/AccelByte/accelbyte-csharp-sdk/blob/main/README.md#usage) for more information||
|AccelByte.Namespace|AccelByte Cloud namespace. [Here](https://github.com/AccelByte/accelbyte-csharp-sdk/blob/main/README.md#usage) for more information||
|AccelByte.AppName|Grpc service application name|MMV2GRPCSERVICE|
|AccelByte.ResourceName|Grpc service resource or instance name|MMV2GRPCSERVICE|

### Environment variables in docker-compose.yml

|Key|Description|
|-|-|
|ASPNETCORE_ENVIRONMENT|ASP.NET Core runtime environment. Read more about it [here](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0). Default is `Development`. Make sure that this value match the name for appsettings json file. E.g. `Development` will read configuration value from `appsettings.Development.json` and `Production` will read configuration value from `appsettings.Production.json`.|
|OTEL_EXPORTER_ZIPKIN_ENDPOINT|[OpenTelemetry zipkin endpoint](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Zipkin/README.md).|
|ASPNETCORE_SERILOG_LOKI|Set Loki instance url to send log directly to Loki. If this var is specified, it will override `LokiUrl` in `appsettings`|