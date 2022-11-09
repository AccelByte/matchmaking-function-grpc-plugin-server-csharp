# AccelByte Plugin Architecture Demo using C# (.NET) [Server Part]

## Setup
1. For complete server components to work, you need `docker` and `docker-compose` to be installed.
2. Install docker logging driver for loki with this command:
    ```bash
    $ docker plugin install grafana/loki-docker-driver:latest --alias loki --grant-all-permissions
    ```
3. You can verify whether loki driver has been installed using:
    ```bash
    $ docker plugin ls
    ```
4. .NET 6.0 SDK is required to build and run outside of docker environment.

## Usage

1. Clone `src/AccelByte.PluginArch.Demo.Server/appsettings.json` to `src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json`
    ```bash
    $ cp src/AccelByte.PluginArch.Demo.Server/appsettings.json src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json
    ```
2. Open `appsettings.Development.json` file and change the configuration values according to your needs. Make sure all `AccelByte` fields are not empty.
3. Run dependencies first.
    ```bash
    $ docker-compose -f docker-compose-dep.yml up
    ```
4. Then run app. Use `--build` if the app image need to be rebuild. For example when there are changes in configuration.
    ```bash
    $ docker-compose -f docker-compose-app.yml up
    or
    $ docker-compose -f docker-compose-app.yml up --build
    ```
5. Use Postman or any other Grpc client, and point it to `localhost:10000` (default). Grpc service discovery is already enabled and if client supported it, then it can be use to simplify the testing.


## Configuration

### appsettings.*.json
|key|description|default|
|-|-|-|
|Kestrel.Endpoints.Http.Url|Prometheus scrapper endpoint|http://0.0.0.0:8080|
|Kestrel.Endpoints.Grpc.Url|Grpc service endpoint|http://0.0.0.0:5500|
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

### Environment Vars in docker-compose-app.yml
|key|description|
|-|-|
|ASPNETCORE_ENVIRONMENT|ASP.NET Core runtime environment. Read more about it [here](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-6.0). Default is `Development`. Make sure that this value match the name for appsettings json file. E.g. `Development` will read configuration value from `appsettings.Development.json` and `Production` will read configuration value from `appsettings.Production.json`.|
|OTEL_EXPORTER_ZIPKIN_ENDPOINT|[OpenTelemetry zipkin endpoint](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Zipkin/README.md).|
|ASPNETCORE_SERILOG_LOKI|Set Loki instance url to send log directly to Loki. If this var is specified, it will override `LokiUrl` in `appsettings`|