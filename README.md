# WeatherApp

WeatherApp is split into two deployable parts:

- `weather-service`: ASP.NET Core weather microservice.
- `weather-web`: React JavaScript web client.

## Current Slice

The first implemented endpoint is:

```http
GET /api/status
```

It returns service status and SQL Server connectivity.

## Run Weather Service

```powershell
cd weather-service
dotnet restore WeatherApp.sln --configfile NuGet.Config
dotnet run --project src/WeatherApp.Api/WeatherApp.Api.csproj
```

Swagger is available when `Swagger:Enabled` is `true`. It is enabled by default in Development and disabled by default in the base production configuration.

```text
/swagger
```
