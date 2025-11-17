# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copies the csproj file from the root of the project into the container's /src folder
COPY ["svc-ai-vision-adapter.csproj", "./"]

# Restores NuGet packages based on the copied project file
RUN dotnet restore "./svc-ai-vision-adapter.csproj"

# Copies the rest of the source files into the container
COPY . .

# Builds the project using the provided build configuration
RUN dotnet build "./svc-ai-vision-adapter.csproj" -c $BUILD_CONFIGURATION -o /app/build


# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

# Publishes the application into a folder suitable for running inside the final image
RUN dotnet publish "./svc-ai-vision-adapter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Copies the published output from the previous stage into the runtime image
COPY --from=publish /app/publish .

# Configures the container to run the .NET application
ENTRYPOINT ["dotnet", "svc-ai-vision-adapter.dll"]
