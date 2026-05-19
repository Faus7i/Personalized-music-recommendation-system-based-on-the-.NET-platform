param(
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$services = @(
    @{
        Name = "Identity API"
        Port = 5143
        Project = "src\MusicRec.Services.Identity.Api\MusicRec.Services.Identity.Api.csproj"
    },
    @{
        Name = "Catalog API"
        Port = 5082
        Project = "src\MusicRec.Services.Catalog.Api\MusicRec.Services.Catalog.Api.csproj"
    },
    @{
        Name = "Recommendation API"
        Port = 5291
        Project = "src\MusicRec.Services.Recommendation.Api\MusicRec.Services.Recommendation.Api.csproj"
    },
    @{
        Name = "Web"
        Port = 5175
        Project = "src\MusicRec.Web\MusicRec.Web.csproj"
    }
)

function Test-PortListening {
    param([int]$Port)

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1

    return $null -ne $connection
}

function Start-ServiceWindow {
    param(
        [string]$ServiceName,
        [string]$ProjectPath
    )

    $command = "Set-Location '$root'; dotnet run --launch-profile http --project '$ProjectPath'"
    Start-Process powershell -WorkingDirectory $root -ArgumentList @(
        "-NoExit",
        "-ExecutionPolicy", "Bypass",
        "-Command", $command
    ) | Out-Null

    Write-Host "[Starting] $ServiceName" -ForegroundColor Green
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet command was not found. Please install .NET 8 SDK first."
}

Write-Host "SonicCanvas one-click start" -ForegroundColor Cyan
Write-Host "Project root: $root"
Write-Host ""

foreach ($service in $services) {
    if (Test-PortListening -Port $service.Port) {
        Write-Host "[Already running] $($service.Name) -> http://localhost:$($service.Port)" -ForegroundColor Yellow
        continue
    }

    Start-ServiceWindow -ServiceName $service.Name -ProjectPath $service.Project
    Start-Sleep -Milliseconds 700
}

Write-Host ""
Write-Host "Startup commands have been sent." -ForegroundColor Cyan
Write-Host "Web URL: http://localhost:5175" -ForegroundColor Cyan

if (-not $NoBrowser) {
    Start-Sleep -Seconds 4
    Start-Process "http://localhost:5175" | Out-Null
}
