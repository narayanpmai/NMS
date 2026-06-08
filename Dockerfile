FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8080

# Install ping utility for NetworkMonitoringSystem
RUN apt-get update && apt-get install -y iputils-ping && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["NetworkMonitoringSystem.Web/NetworkMonitoringSystem.Web.csproj", "NetworkMonitoringSystem.Web/"]
COPY ["NetworkMonitoringSystem.Application/NetworkMonitoringSystem.Application.csproj", "NetworkMonitoringSystem.Application/"]
COPY ["NetworkMonitoringSystem.Domain/NetworkMonitoringSystem.Domain.csproj", "NetworkMonitoringSystem.Domain/"]
COPY ["NetworkMonitoringSystem.Infrastructure/NetworkMonitoringSystem.Infrastructure.csproj", "NetworkMonitoringSystem.Infrastructure/"]
RUN dotnet restore "NetworkMonitoringSystem.Web/NetworkMonitoringSystem.Web.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/NetworkMonitoringSystem.Web"
RUN dotnet build "NetworkMonitoringSystem.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetworkMonitoringSystem.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Run the app
ENTRYPOINT ["dotnet", "NetworkMonitoringSystem.Web.dll"]
