FROM mcr.microsoft.com/dotnet/sdk:6.0.417
WORKDIR /workspace
RUN apt update && apt install -y bash curl docker git jq make xxd \
        && apt clean \
        && rm -rf /var/lib/apt/lists/*
COPY --from=docker:dind /usr/local/bin/docker /usr/local/bin/
COPY --from=ngrok/ngrok /bin/ngrok /bin/ngrok
