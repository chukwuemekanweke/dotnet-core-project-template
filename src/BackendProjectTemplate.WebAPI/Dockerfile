FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore BackendProjectTemplate.slnx
RUN dotnet publish src/BackendProjectTemplate.WebAPI/BackendProjectTemplate.WebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BackendProjectTemplate.WebAPI.dll"]
