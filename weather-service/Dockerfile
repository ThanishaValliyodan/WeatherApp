FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WeatherApp.sln ./
COPY NuGet.Config ./
COPY src/WeatherApp.Domain/WeatherApp.Domain.csproj src/WeatherApp.Domain/
COPY src/WeatherApp.Application/WeatherApp.Application.csproj src/WeatherApp.Application/
COPY src/WeatherApp.Infrastructure/WeatherApp.Infrastructure.csproj src/WeatherApp.Infrastructure/
COPY src/WeatherApp.Api/WeatherApp.Api.csproj src/WeatherApp.Api/

RUN dotnet restore src/WeatherApp.Api/WeatherApp.Api.csproj --configfile NuGet.Config

COPY . .
RUN dotnet publish src/WeatherApp.Api/WeatherApp.Api.csproj \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "WeatherApp.Api.dll"]
