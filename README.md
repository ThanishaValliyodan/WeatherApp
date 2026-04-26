# WeatherApp

WeatherApp is a full-stack Singapore weather application built for the weather microservice challenge.

The solution has two deployable parts:

- `weather-service`: ASP.NET Core 8 weather microservice using Clean Architecture, SQL Server, EF Core, Swagger/OpenAPI, rate limiting, health checks, and data.gov.sg integration.
- `weather-web`: React web client that calls only `weather-service`.

The frontend never calls data.gov.sg directly and never receives the data.gov.sg API key.

## Features

- Current weather by Singapore location.
- Forecasts with provider-accurate scope:
  - 2-hour forecast by selected forecast area.
  - 24-hour Singapore overall and regional outlook.
  - 4-day Singapore-wide outlook.
- Stored historical weather lookup.
- CSV export for historical weather records.
- Weather alert subscriptions with manual alert evaluation.
- Protected historical sync endpoint.
- Health/status endpoints.
- Dockerfiles and Docker Compose for local container runs.
- GitHub Actions CI for backend build/tests and frontend build.

## Architecture

The backend follows Clean Architecture:

```text
WeatherApp.Api -> WeatherApp.Application -> WeatherApp.Domain
WeatherApp.Infrastructure -> WeatherApp.Application -> WeatherApp.Domain
```

Responsibilities:

- `WeatherApp.Api`: Minimal API endpoints, middleware, Swagger, CORS, rate limiting, health checks.
- `WeatherApp.Application`: DTOs, abstractions, and deterministic business rules.
- `WeatherApp.Domain`: Entities.
- `WeatherApp.Infrastructure`: EF Core, SQL Server, data.gov.sg client, sync/query services, persistence.
- `weather-web`: React pages and API clients for dashboard, forecasts, history/export, and alerts.

## Prerequisites

- .NET 8 SDK
- Node.js 20
- SQL Server
  - Windows authentication for direct local backend runs
  - SQL authentication enabled for Docker Compose runs
- data.gov.sg API key
- Docker Desktop, optional

## Configuration

Backend configuration keys:

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `DataGovSg:ApiKey` | data.gov.sg API key |
| `AdminApiKey` | Secret key for protected historical sync |
| `AllowedOrigins` | Allowed frontend origins for CORS |
| `Swagger:Enabled` | Enables or disables Swagger UI/OpenAPI |
| `PORT` | Optional HTTP port for container/cloud hosting |

Recommended local user secrets:

```powershell
cd weather-service/src/WeatherApp.Api
dotnet user-secrets init
dotnet user-secrets set "DataGovSg:ApiKey" "<your-data-gov-sg-api-key>"
dotnet user-secrets set "AdminApiKey" "<your-admin-key>"
```

The default direct local connection string in `appsettings.json` uses Windows authentication:

```text
Server=localhost;Database=WeatherApp;Trusted_Connection=True;TrustServerCertificate=True;
```

Set `ConnectionStrings:DefaultConnection` in user secrets only if you want to override that default.

### Obtaining API Keys

To run the solution locally, obtain the required API keys and store them in local configuration.

data.gov.sg API key:

- Create or log in to a data.gov.sg account.
- Follow the official guide: [How to request an API key](https://guide.data.gov.sg/developer-guide/api-overview/how-to-request-an-api-key).
- Create a Developer or Production key based on the environment.
- Copy the key when it is generated and store it securely. data.gov.sg notes that generated keys may not be viewable again.
- The backend sends this key to data.gov.sg using the `x-api-key` header.

Admin API key:

- This is an application-owned secret for protecting WeatherApp admin sync endpoints.
- It is not provided by data.gov.sg.
- Generate a random value locally and store it as `AdminApiKey`.

Example PowerShell command to generate an admin key:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Store both keys in .NET user secrets for local backend runs:

```powershell
cd weather-service/src/WeatherApp.Api
dotnet user-secrets set "DataGovSg:ApiKey" "<your-data-gov-sg-api-key>"
dotnet user-secrets set "AdminApiKey" "<generated-admin-api-key>"
```

For Docker Compose, configure environment variables before running:

```powershell
$env:SQL_PASSWORD="<your-sql-password>"
$env:DATA_GOV_SG_API_KEY="<your-data-gov-sg-api-key>"
$env:ADMIN_API_KEY="<generated-admin-api-key>"
```

Docker Compose maps those environment variables into the backend container configuration. The Docker Compose connection string uses SQL authentication against the SQL Server instance on `host.docker.internal`:

```text
Server=host.docker.internal;Database=WeatherApp;User Id=thanisha;Password=<SQL_PASSWORD>;TrustServerCertificate=True;
```

Use the same `ADMIN_API_KEY` value in Swagger UI's **Authorize** dialog when testing protected sync endpoints.

## Run Locally

Run the backend:

```powershell
cd weather-service
dotnet restore WeatherApp.sln --configfile NuGet.Config
dotnet run --project src/WeatherApp.Api/WeatherApp.Api.csproj
```

Run the frontend:

```powershell
cd weather-web
npm install
npm run dev
```

Default local URLs:

- Frontend: `http://localhost:5173`
- Backend: use the URL printed by `dotnet run`
- Docker backend: `http://localhost:5000`

Swagger is config-driven:

- Enabled in Development through `appsettings.Development.json`.
- Disabled by default in base `appsettings.json`.
- Can be enabled with `Swagger__Enabled=true`.

Swagger URL:

```text
/swagger
```

## Run With Docker Compose

```powershell
docker compose up --build
```

Docker Compose exposes:

- `weather-service`: `http://localhost:5000`
- `weather-web`: `http://localhost:5173`

The Compose file expects SQL Server to be available from the container as `host.docker.internal`.

## API Endpoints

Health and status:

```http
GET /health
GET /health/live
GET /health/ready
GET /api/status
```

Locations:

```http
GET /api/locations
GET /api/locations?query=ang
```

Weather:

```http
GET /api/weather/current?location=Ang%20Mo%20Kio
GET /api/weather/forecast?type=two-hour&location=Ang%20Mo%20Kio
GET /api/weather/forecast?type=twenty-four-hour
GET /api/weather/forecast?type=twenty-four-hour&region=west
GET /api/weather/forecast?type=four-day
GET /api/weather/history?location=Ang%20Mo%20Kio&from=2026-04-01&to=2026-04-26
GET /api/weather/export?location=Ang%20Mo%20Kio&from=2026-04-01&to=2026-04-26
```

Forecast scope:

- `two-hour` uses the selected forecast area, such as `Ang Mo Kio`.
- `twenty-four-hour` is not location-specific. Without `region`, it returns Singapore overall plus all regional periods. With `region`, it filters the regional periods while still including the Singapore overall summary.
- `four-day` is Singapore-wide and does not use `location` or `region`.

Date and timestamp behavior:

- Request date parameters such as `from`, `to`, and sync `date` are plain Singapore calendar dates in `YYYY-MM-DD` format.
- Historical lookup and CSV export convert the selected Singapore date range to UTC before querying stored records.
- Response fields ending in `Utc` are UTC-normalized timestamps.
- The frontend converts UTC timestamps back to Singapore time for display.

Alerts:

```http
POST /api/alerts/subscriptions
GET /api/alerts/subscriptions
GET /api/alerts/subscriptions/{id}
DELETE /api/alerts/subscriptions/{id}
POST /api/alerts/evaluate?location=Ang%20Mo%20Kio
```

Protected sync:

```http
POST /api/weather/history/sync?date=2026-04-26
POST /api/weather/history/sync?from=2026-04-01&to=2026-04-07
POST /api/weather/history/sync?months=3
POST /api/weather/history/sync?months=3&force=true
GET /api/weather/history/sync/runs/{id}
```

Protected sync requires:

```http
x-admin-api-key: <AdminApiKey>
```

In Swagger UI, click **Authorize** and enter the configured `AdminApiKey` value to send the `x-admin-api-key` header for protected sync requests.

The sync `POST` endpoint returns `202 Accepted` with a sync run id. Use `GET /api/weather/history/sync/runs/{id}` to check whether the run is `Queued`, `Running`, `Succeeded`, or `Failed`.

## Tests

Backend unit tests use xUnit and focus on deterministic business rules.

Run backend tests:

```powershell
cd weather-service
dotnet test WeatherApp.sln --configuration Release
```

Current test coverage includes:

- Alert subscription validation.
- Alert type normalization.
- Alert trigger evaluation.
- Sync date range validation.

Run frontend build verification:

```powershell
cd weather-web
npm run build
```

## CI

GitHub Actions CI runs on push and pull request to `main`.

The CI pipeline:

- Restores the backend solution.
- Builds the backend.
- Runs xUnit tests.
- Uploads backend `.trx` test results as an artifact.
- Installs frontend dependencies.
- Builds the frontend.

## Security And Resiliency

Implemented security and resiliency measures:

- Secrets are configured through user secrets, environment variables, or cloud configuration.
- data.gov.sg API key stays server-side only.
- CORS is restricted to configured frontend origins.
- Swagger exposure is config-driven and disabled by default in production configuration.
- Historical sync endpoint is protected by `x-admin-api-key`.
- Endpoint-specific rate limiting is configured for public, alerts, and admin sync APIs.
- Global exception middleware returns safe error responses.
- Security headers middleware adds defensive browser headers.
- EF Core SQL Server transient retry is enabled.
- Current weather provider calls are parallelized and cached briefly.
- Provider failures return safe responses instead of leaking stack traces.
- Historical reads come from SQL Server instead of repeatedly calling data.gov.sg.

## Database

The backend uses SQL Server and EF Core migrations.

On startup, the API applies pending migrations with `MigrateAsync`. This is convenient for local/demo deployments. For stricter production deployments, run migrations as a controlled deployment step before starting the app.

## Notes

- Alert delivery is stored/evaluation-only. Email or SMS delivery is intentionally outside the current scope.
- Historical sync currently records provider sync checkpoints and persisted data required by the implemented history/export flows.
