# Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
# This is licensed software from AccelByte Inc, for limitations
# and restrictions contact your company contract manager.

SHELL := /bin/bash

.PHONY: build test

build:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:6.0 dotnet build

test:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:6.0 dotnet test