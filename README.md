# matchmaking-function-grpc-plugin-server-csharp

```mermaid
flowchart LR
   subgraph AB Cloud Service
   CL[gRPC Client]
   end
   subgraph External Hosting
   SV["gRPC Server\n(YOU ARE HERE)"]
   DS[Dependency Services]
   CL --- DS
   end
   DS --- SV
```

`AccelByte Gaming Services` capabilities can be extended using custom functions implemented in a `gRPC server`.
If configured, custom functions in the `gRPC server` will be called by `AccelByte Gaming Services` instead of the default function.

The `gRPC server` and the `gRPC client` can actually communicate directly. 
However, additional services are necessary to provide **security**, **reliability**, **scalability**, and **observability**. 
We call these services as `dependency services`. 
The [grpc-plugin-dependencies](https://github.com/AccelByte/grpc-plugin-dependencies) repository is provided 
as an example of what these `dependency services` may look like. 
It contains a docker compose which consists of these `dependency services`.

> :warning: **grpc-plugin-dependencies is provided as example for local development purpose only:** The dependency services in the actual gRPC server deployment may not be exactly the same.

## Overview

This repository contains `sample matchmaking function gRPC server app` written in `C#`, It provides simple custom
matchmaking function implementation for matchmaking service in `AccelByte Gaming Services`. 
It will simply match 2 players coming into the function.

This sample app also shows how this `gRPC server` can be instrumented for better observability.
It is configured by default to send metrics, traces, and logs to the observability `dependency services` 
in [grpc-plugin-dependencies](https://github.com/AccelByte/grpc-plugin-dependencies).

## Prerequisites

1. Windows 10 WSL2 or Linux Ubuntu 20.04 with the following tools installed.

   a. bash

   b. make

   c. docker v23.x

   d. docker-compose v2

   e. .net 6 sdk

   f. docker loki driver
    
      ```
      docker plugin install grafana/loki-docker-driver:latest --alias loki --grant-all-permissions
      ```
   g. [ngrok](https://ngrok.com/)

   h. [postman](https://www.postman.com/)

2. A local copy of [grpc-plugin-dependencies](https://github.com/AccelByte/grpc-plugin-dependencies) repository.

   ```
   git clone https://github.com/AccelByte/grpc-plugin-dependencies.git
   ```

3. Access to `AccelByte Gaming Services` demo environment.

   a. Base URL: https://demo.accelbyte.io

   b. [Create a Game Namespace](https://docs.accelbyte.io/esg/uam/namespaces.html#tutorials) if you don't have one yet. Keep the `Namespace ID`.

   c. [Create an OAuth Client](https://docs.accelbyte.io/guides/access/iam-client.html) with confidential client type with the following permission. Keep the `Client ID` and `Client Secret`.

      - NAMESPACE:{namespace}:MMV2GRPCSERVICE [READ]


## Setup

To be able to run this sample app, you will need to follow these setup steps.

1. Create a docker compose `.env` file by copying the content of [.env.template](.env.template) file.
2. Fill in the required environment variables in `.env` file as shown below.

   ```
   AB_BASE_URL=https://demo.accelbyte.io     # Base URL of AccelByte Gaming Services demo environment
   AB_CLIENT_ID='xxxxxxxxxx'                 # Client ID from the Prerequisites section
   AB_CLIENT_SECRET='xxxxxxxxxx'             # Client Secret from the Prerequisites section
   AB_NAMESPACE='xxxxxxxxxx'                 # Namespace ID from the Prerequisites section
   PLUGIN_GRPC_SERVER_AUTH_ENABLED=false     # Enable or disable access token and permission verification
   ```

   > :warning: **Keep PLUGIN_GRPC_SERVER_AUTH_ENABLED=false for now**: It is currently not
   supported by `AccelByte Gaming Services`, but it will be enabled later on to improve security. If it is
   enabled, the gRPC server will reject any calls from gRPC clients without proper authorization
   metadata.

For more options, create `src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json` and fill in the required configuration.

```json
{
  "DirectLogToLoki": false,
  "EnableAuthorization": false,                 // Enable or disable access token and permission check (env var: PLUGIN_GRPC_SERVER_AUTH_ENABLED)
  "RevocationListRefreshPeriod": 60,
  "AccelByte": {
    "BaseUrl": "https://demo.accelbyte.io",     // Base URL (env var: AB_BASE_URL)
    "ClientId": "xxxxxxxxxx",                   // Client ID (env var: AB_CLIENT_ID)    
    "ClientSecret": "xxxxxxxxxx",               // Client Secret (env var: AB_CLIENT_SECRET)
    "AppName": "MMV2GRPCSERVICE",
    "TraceIdVersion": "1",
    "Namespace": "xxxxxxxxxx",                  // Namespace ID (env var: AB_NAMESPACE)
    "EnableTraceId": true,
    "EnableUserAgentInfo": true,
    "ResourceName": "MMV2GRPCSERVICE"
  }
}
```
> :warning: **Environment variable values will override related configuration values in this file**.

## Building

To build this sample app, use the following command.

```
make build
```

## Running

To (build and) run this sample app in a container, use the following command.

```
docker-compose up --build
```

## Testing

### Functional Test in Local Development Environment

The custom functions in this sample app can be tested locally using `postman`.

1. Run the `dependency services` by following the `README.md` in the [grpc-plugin-dependencies](https://github.com/AccelByte/grpc-plugin-dependencies) repository.
   > :warning: **Make sure to start dependency services with mTLS disabled for now**: It is currently not supported by `AccelByte Gaming Services`, but it will be enabled later on to improve security. If it is enabled, the gRPC client calls without mTLS will be rejected.

2. Run this `gRPC server` sample app by using command below.
   ```shell
   docker-compose up --build
   ```

3. Open `postman`, create a new `gRPC request` (tutorial [here](https://blog.postman.com/postman-now-supports-grpc/)), and enter `localhost:10000` as server URL. 

   > :exclamation: We are essentially accessing the `gRPC server` through an `Envoy` proxy in `dependency services`.

4. In `postman`, continue by selecting `MakeMatches` grpc stream method and click `Invoke` button, this will start stream connection to grpc server sample app.
5. In `postman`, continue sending parameters first to specify number of players in a match by copying sample `json` below and click `Send`.

   ```json
   {
       "parameters": {
           "rules": {
               "json": "{\"shipCountMin\":2, \"shipCountMax\":2}"
           }
       }
   }
   ```

6. Still In `postman`, now we can send match ticket to start matchmaking by copying sample `json` below and replace it into `postman` message then click `Send`

   ```json
   {
       "ticket": {
           "players": [
               {
                   "player_id": "playerA"
               }
           ]
       }
   }
   ```

7. You can do step *6* multiple times until the number of player met and find matches, in our case it is 2 players.

8. If successful, you will receive response (down stream) in `postman` similar to `json` sample below

   ```json
   {
       "match": {
           "tickets": [],
           "teams": [
               {
                   "user_ids": [
                       "playerA",
                       "playerB"
                   ]
               }
           ],
           "region_preferences": [
               "any"
           ],
           "match_attributes": null
       }
   }
   ```


### Integration Test with AccelByte Gaming Services

After passing functional test in local development environment, you may want to perform
integration test with `AccelByte Gaming Services`. Here, we are going to expose the `gRPC server`
in local development environment to the internet so that it can be called by
`AccelByte Gaming Services`. To do this without requiring public IP, we can use [ngrok](https://ngrok.com/)

1. Run the `dependency services` by following the `README.md` in the [grpc-plugin-dependencies](https://github.com/AccelByte/grpc-plugin-dependencies) repository.
   > :warning: **Make sure to start dependency services with mTLS disabled for now**: It is currently not supported by `AccelByte Gaming Services`, but it will be enabled later on to improve security. If it is enabled, the gRPC client calls without mTLS will be rejected.

2. Run this `gRPC server` sample app by using command below.
   ```shell
   docker-compose up
   ```

3. Sign-in/sign-up to [ngrok](https://ngrok.com/) and get your auth token in `ngrok` dashboard.

4. In `grpc-plugin-dependencies` repository, run the following command to expose `gRPC server` Envoy proxy port in local development environment to the internet. Take a note of the `ngrok` forwarding URL e.g. `http://0.tcp.ap.ngrok.io:xxxxx`.

   ```
   make ngrok NGROK_AUTHTOKEN=xxxxxxxxxxx
   ```

5. [Create an OAuth Client](https://docs.accelbyte.io/guides/access/iam-client.html) with `confidential` client type with the following permissions. Keep the `Client ID` and `Client Secret`.

   - NAMESPACE:{namespace}:MATCHMAKING:RULES [CREATE, READ, UPDATE, DELETE]
   - NAMESPACE:{namespace}:MATCHMAKING:FUNCTIONS [CREATE, READ, UPDATE, DELETE]
   - NAMESPACE:{namespace}:MATCHMAKING:POOL [CREATE, READ, UPDATE, DELETE]
   - NAMESPACE:{namespace}:MATCHMAKING:TICKET [CREATE, READ, UPDATE, DELETE]
   - ADMIN:NAMESPACE:{namespace}:INFORMATION:USER:* [CREATE, READ, UPDATE, DELETE]
   - ADMIN:NAMESPACE:{namespace}:SESSION:CONFIGURATION:* [CREATE, READ, UPDATE, DELETE]

   > :warning: **Oauth Client created in this step is different from the one from Prerequisites section:** It is required by [demo.sh](demo.sh) script in the next step to register the `gRPC Server` URL and also to create and delete test users.
   
6. Run the [demo.sh](demo.sh) script to simulate the matchmaking flow which calls this sample `gRPC server` using the `Client ID` and `Client Secret` created in the previous step. Pay attention to sample `gRPC server` console log when matchmaking flow is running. `gRPC Server` methods should get called when creating match tickets and it should group players in twos.

   ```
   export AB_BASE_URL='https://demo.accelbyte.io'
   export AB_CLIENT_ID='xxxxxxxxxx'         # Use Client ID from the previous step
   export AB_CLIENT_SECRET='xxxxxxxxxx'     # Use Client Secret from the previous step    
   export AB_NAMESPACE='accelbyte'          # Use your Namespace ID
   export GRPC_SERVER_URL='http://0.tcp.ap.ngrok.io:xxxxx'  # Use your ngrok forwarding URL
   bash demo.sh
   ```

   > :warning: **Make sure demo.sh has Unix line-endings (LF)**: If this repository was cloned in Windows for example, the `demo.sh` may have Windows line-endings (CRLF) instead. In this case, use tools like `dos2unix` to change the line-endings to Unix (LF).
   Invalid line-endings may cause errors such as `demo.sh: line 2: $'\r': command not found`.
 
> :warning: **Ngrok free plan has some limitations**: You may want to use paid plan if the traffic is high.

### Deploy to AccelByte Gaming Services

After passing integration test against locally running sample app you may want to deploy the sample app to AGS (AccelByte Gaming Services).

1. Download and setup [extend-helper-cli](https://github.com/AccelByte/extend-helper-cli/)
2. Create new Extend App on Admin Portal, please refer to docs [here](https://docs-preview.accelbyte.io/gaming-services/services/customization/using-custom-matchmaking/)
3. Do docker login using `extend-helper-cli`, please refer to its documentation
4. Build and push sample app docker image to AccelByte ECR using the following command inside sample app directory
   ```
   make imagex_push REPO_URL=xxxxxxxxxx.dkr.ecr.us-west-2.amazonaws.com/accelbyte/justice/development/extend/xxxxxxxxxx/xxxxxxxxxx IMAGE_TAG=v0.0.1
   ```
   > Note: the REPO_URL is obtained from step 2 in the app detail on the 'Repository Url' field

Please refer to [getting started docs](https://docs-preview.accelbyte.io/gaming-services/services/customization/using-custom-matchmaking/) for more detailed steps on how to deploy sample app to AccelByte Gaming Service.

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