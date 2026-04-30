# See https://aka.ms/customizecontainer

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Timesheets.Api/Timesheets.Api.csproj", "Timesheets.Api/"]
RUN dotnet restore "./Timesheets.Api/Timesheets.Api.csproj"
COPY . .
WORKDIR "/src/Timesheets.Api"
RUN dotnet build "./Timesheets.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Timesheets.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS prebuilt
WORKDIR /app
COPY .ci/publish/ .
ENTRYPOINT ["dotnet", "Timesheets.Api.dll"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Timesheets.Api.dll"]
