# Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
# This is licensed software from AccelByte Inc, for limitations
# and restrictions contact your company contract manager.

SHELL := /bin/bash

IMAGE_NAME := plugin-arch-grpc-server-csharp-app
DOTNETVER := 6.0.302

.PHONY: build image imagex test

build:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e HOME="/data" -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:$(DOTNETVER) \
			dotnet build

image:
	docker build -t ${IMAGE_NAME} .

imagex:
	trap "docker buildx rm ${IMAGE_NAME}-builder" EXIT \
			&& docker buildx create --name ${IMAGE_NAME}-builder --use \
			&& docker buildx build -t ${IMAGE_NAME} --platform linux/arm64/v8,linux/amd64 . \
			&& docker buildx build -t ${IMAGE_NAME} --load .

test:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e HOME="/data" -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:$(DOTNETVER) \
			dotnet test