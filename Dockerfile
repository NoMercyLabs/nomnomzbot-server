# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) NoMercy Entertainment. All rights reserved.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/NomercyBot.Domain/NomercyBot.Domain.csproj", "src/NomercyBot.Domain/"]
COPY ["src/NomercyBot.Application/NomercyBot.Application.csproj", "src/NomercyBot.Application/"]
COPY ["src/NomercyBot.Infrastructure/NomercyBot.Infrastructure.csproj", "src/NomercyBot.Infrastructure/"]
COPY ["src/NomercyBot.Api/NomercyBot.Api.csproj", "src/NomercyBot.Api/"]

RUN dotnet restore "src/NomercyBot.Api/NomercyBot.Api.csproj"

COPY . .
WORKDIR "/src/src/NomercyBot.Api"
RUN dotnet build "NomercyBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NomercyBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NomercyBot.Api.dll"]
