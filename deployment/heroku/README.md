# Heroku Infrastructure

WeatherApp uses a microservice-style Heroku deployment:

- `WeatherApp.Api` deploys to its own Heroku app.
- `WeatherApp.Web` deploys to its own Heroku app.
- Heroku MSSQL is attached to the API app.
- `weather-web` calls `weather-service` through a configured API base URL.

## Estimated Monthly Cost

For keeping the environment alive for one month:

- API Basic dyno: about `$7/month`
- Frontend Basic dyno: about `$7/month`
- Heroku MSSQL Micro add-on: maximum about `$15/month`

Estimated total: about `$29/month`, before taxes or currency conversion.

The lower-cost variant is one API dyno plus MSSQL, with `weather-web` hosted somewhere else or served by the API, for about `$22/month`. This project uses separate Heroku apps to keep the deployment boundary clearer.

## GitHub Requirements

Add these repository secrets:

```text
HEROKU_API_KEY
```

Add these repository variables:

```text
HEROKU_API_APP_NAME
HEROKU_WEB_APP_NAME
```

## Heroku Config Vars

API app:

```text
ASPNETCORE_ENVIRONMENT=Production
DataGovSg__ApiKey=<real data.gov.sg key value>
AdminApiKey=<generated admin key>
AllowedOrigins__0=https://<weather-web-app-name>.herokuapp.com
ConnectionStrings__DefaultConnection=<provided by Heroku MSSQL add-on>
```

Frontend app:

```text
VITE_API_BASE_URL=https://<api-app-name>.herokuapp.com
```

## Provisioning Commands

Replace the app names before running.

```powershell
$apiApp = "weatherapp-api-yourname"
$webApp = "weatherapp-web-yourname"
$region = "us"

heroku create $apiApp --region $region
heroku create $webApp --region $region

heroku addons:create mssql:micro --app $apiApp

heroku config:set ASPNETCORE_ENVIRONMENT=Production --app $apiApp
heroku config:set DataGovSg__ApiKey="<data.gov.sg api key value>" --app $apiApp
heroku config:set AdminApiKey="<generated admin key>" --app $apiApp
heroku config:set AllowedOrigins__0="https://$webApp.herokuapp.com" --app $apiApp

heroku config:set VITE_API_BASE_URL="https://$apiApp.herokuapp.com" --app $webApp
```

## Deployment Flow

1. Push to `main`.
2. GitHub Actions runs weather-service and weather-web CI.
3. GitHub Actions deploys API container to `HEROKU_API_APP_NAME`.
4. GitHub Actions deploys weather-web container to `HEROKU_WEB_APP_NAME`.

Deployment starts working after these files are added:

```text
weather-service/Dockerfile
weather-web/Dockerfile
```
