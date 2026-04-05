# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) NoMercy Entertainment. All rights reserved.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/NoMercyBot.Domain/NoMercyBot.Domain.csproj", "src/NoMercyBot.Domain/"]
COPY ["src/NoMercyBot.Application/NoMercyBot.Application.csproj", "src/NoMercyBot.Application/"]
COPY ["src/NoMercyBot.Infrastructure/NoMercyBot.Infrastructure.csproj", "src/NoMercyBot.Infrastructure/"]
COPY ["src/NoMercyBot.Api/NoMercyBot.Api.csproj", "src/NoMercyBot.Api/"]

RUN dotnet restore "src/NoMercyBot.Api/NoMercyBot.Api.csproj"

COPY . .
WORKDIR "/src/src/NoMercyBot.Api"
RUN dotnet build "NoMercyBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NoMercyBot.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoMercyBot.Api.dll"]
