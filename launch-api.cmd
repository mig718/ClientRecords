@echo off
set "SWAGGER_URL=https://localhost:7290/swagger"

REM dotnet run may not auto-launch browser from launchSettings in CLI usage.
start "" "%SWAGGER_URL%"

dotnet run --launch-profile https
