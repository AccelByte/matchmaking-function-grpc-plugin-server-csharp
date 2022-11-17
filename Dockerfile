FROM mcr.microsoft.com/dotnet/sdk:6.0-focal

ARG PROJECT_SRC_PATH=src/AccelByte.PluginArch.Demo.Server

WORKDIR /app-build
COPY $PROJECT_SRC_PATH/*.csproj ./
RUN dotnet restore

COPY $PROJECT_SRC_PATH ./
RUN dotnet publish -c Release -o out

WORKDIR /app
RUN cp -r /app-build/out/* ./

RUN chmod 0777 /app/AccelByte.PluginArch.Demo.Server

EXPOSE 6565
ENTRYPOINT ["/app/AccelByte.PluginArch.Demo.Server"]