# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/BackendProjectTemplate.WebAPI/BackendProjectTemplate.WebAPI.csproj", "src/BackendProjectTemplate.WebAPI/"]
COPY ["src/BackendProjectTemplate.Application/BackendProjectTemplate.Application.csproj", "src/BackendProjectTemplate.Application/"]
COPY ["src/BackendProjectTemplate.Contracts/BackendProjectTemplate.Contracts.csproj", "src/BackendProjectTemplate.Contracts/"]
COPY ["src/BackendProjectTemplate.Domain/BackendProjectTemplate.Domain.csproj", "src/BackendProjectTemplate.Domain/"]
COPY ["src/BackendProjectTemplate.Infrastructure/BackendProjectTemplate.Infrastructure.csproj", "src/BackendProjectTemplate.Infrastructure/"]
RUN dotnet restore "./src/BackendProjectTemplate.WebAPI/BackendProjectTemplate.WebAPI.csproj"
COPY . .
WORKDIR "/src/src/BackendProjectTemplate.WebAPI"
RUN dotnet build "./BackendProjectTemplate.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BackendProjectTemplate.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackendProjectTemplate.WebAPI.dll"]