# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj (root-level)
COPY ["svc-ai-vision-adapter.csproj", "./"]

# Restore dependencies
RUN dotnet restore "./svc-ai-vision-adapter.csproj"

# Copy everything else
COPY . .

# Build
RUN dotnet build "./svc-ai-vision-adapter.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./svc-ai-vision-adapter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "svc-ai-vision-adapter.dll"]
