# AccelByte Plugin Architecture Demo using C# (.NET) [Server Part]

## Setup
1. This demo requires .NET 6.0 SDK to be installed.
2. For complete server components to work, you need `docker` and `docker-compose` to be installed.
3. Install docker logging driver for loki with this command:
    ```bash
    $ docker plugin install grafana/loki-docker-driver:latest --alias loki --grant-all-permissions
    ```
4. You can verify whether loki driver has been installed using:
    ```bash
    $ docker plugin ls
    ```

## Usage

1. Clone `src/AccelByte.PluginArch.Demo.Server/appsettings.json` to `src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json`
    ```bash
    $ cp src/AccelByte.PluginArch.Demo.Server/appsettings.json src/AccelByte.PluginArch.Demo.Server/appsettings.Development.json
    ```
2. Set AccelByte configuration parameters.
3. Run dependencies first.
    ```bash
    $ docker-compose -f docker-compose-dep.yml up
    ```
4. Then run app. Use `--build` if the app image need to be rebuild.
    ```bash
    $ docker-compose -f docker-compose-app.yml up
    or
    $ docker-compose -f docker-compose-app.yml up --build
    ```
5. Use Postman or any other Grpc client, and point it to `localhost:10000` (default). Grpc service discovery is already enabled and if client supported it, then it can be use to simplify the testing.