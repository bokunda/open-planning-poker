# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/GameEngine/OpenPlanningPoker.GameEngine.GraphQL/OpenPlanningPoker.GameEngine.GraphQL.csproj", "src/GameEngine/OpenPlanningPoker.GameEngine.GraphQL/"]
COPY ["src/GameEngine/OpenPlanningPoker.GameEngine.Application/OpenPlanningPoker.GameEngine.Application.csproj", "src/GameEngine/OpenPlanningPoker.GameEngine.Application/"]
COPY ["src/GameEngine/OpenPlanningPoker.GameEngine.Domain/OpenPlanningPoker.GameEngine.Domain.csproj", "src/GameEngine/OpenPlanningPoker.GameEngine.Domain/"]
COPY ["src/GameEngine/OpenPlanningPoker.GameEngine.Infrastructure/OpenPlanningPoker.GameEngine.Infrastructure.csproj", "src/GameEngine/OpenPlanningPoker.GameEngine.Infrastructure/"]
RUN dotnet restore "./src/GameEngine/OpenPlanningPoker.GameEngine.GraphQL/OpenPlanningPoker.GameEngine.GraphQL.csproj"
COPY . .
WORKDIR "/src/src/GameEngine/OpenPlanningPoker.GameEngine.GraphQL"
RUN dotnet build "./OpenPlanningPoker.GameEngine.GraphQL.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenPlanningPoker.GameEngine.GraphQL.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenPlanningPoker.GameEngine.GraphQL.dll"]