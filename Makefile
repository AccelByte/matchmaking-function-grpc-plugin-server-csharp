# Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
# This is licensed software from AccelByte Inc, for limitations
# and restrictions contact your company contract manager.

SHELL := /bin/bash

DOTNETVER := 6.0.302

.PHONY: build test

build:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e HOME="/data" -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:$(DOTNETVER) dotnet build

test:
	docker run --rm -u $$(id -u):$$(id -g) -v $$(pwd)/src:/data/ -w /data/ -e HOME="/data" -e DOTNET_CLI_HOME="/data" mcr.microsoft.com/dotnet/sdk:$(DOTNETVER) dotnet test