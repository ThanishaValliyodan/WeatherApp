param(
    [Parameter(Mandatory = $true)]
    [string] $ApiAppName,

    [Parameter(Mandatory = $true)]
    [string] $WebAppName,

    [Parameter(Mandatory = $true)]
    [string] $DataGovSgApiKey,

    [Parameter(Mandatory = $true)]
    [string] $AdminApiKey,

    [string] $Region = "us"
)

$ErrorActionPreference = "Stop"

heroku create $ApiAppName --region $Region
heroku create $WebAppName --region $Region

heroku addons:create mssql:micro --app $ApiAppName

heroku config:set ASPNETCORE_ENVIRONMENT=Production --app $ApiAppName
heroku config:set DataGovSg__ApiKey=$DataGovSgApiKey --app $ApiAppName
heroku config:set AdminApiKey=$AdminApiKey --app $ApiAppName
heroku config:set AllowedOrigins__0="https://$WebAppName.herokuapp.com" --app $ApiAppName

heroku config:set VITE_API_BASE_URL="https://$ApiAppName.herokuapp.com" --app $WebAppName

Write-Host "Heroku infrastructure provisioned."
Write-Host "API app: https://$ApiAppName.herokuapp.com"
Write-Host "Web app: https://$WebAppName.herokuapp.com"

