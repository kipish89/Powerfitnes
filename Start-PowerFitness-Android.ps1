$apiPath = "C:\Users\User\Documents\New project\PowerFitness.Api"
$botPath = "C:\Users\User\Documents\New project\telegram_bridge"

$botToken = "8268119707:AAF_jZsbhoChaJf-VZaKETaFnb9T9V1dc-g"
$providerToken = "1744374395:TEST:e622a5a9c69996bd9809"
$botUsername = "iktrainingbot"
$apiUrl = "http://localhost:5004"
$androidApiUrl = "http://192.168.1.5:5004"

Write-Host "Android device API URL: $androidApiUrl" -ForegroundColor Cyan

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$apiPath'; dotnet run --urls http://0.0.0.0:5004"
)

Start-Sleep -Seconds 3

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "`$env:TELEGRAM_BOT_TOKEN='$botToken'; " +
    "`$env:TELEGRAM_PROVIDER_TOKEN='$providerToken'; " +
    "`$env:TELEGRAM_BOT_USERNAME='$botUsername'; " +
    "`$env:POWERFITNESS_API_URL='$apiUrl'; " +
    "Set-Location '$botPath'; py bot.py"
)
