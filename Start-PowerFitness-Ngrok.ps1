param(
    [string]$NgrokCommand = "ngrok",
    [string]$ReservedUrl = "",
    [string]$NgrokConfig = "C:\Users\User\Documents\New project\ngrok.yml"
)

$apiPath = "C:\Users\User\Documents\New project\PowerFitness.Api"
$botPath = "C:\Users\User\Documents\New project\telegram_bridge"

$botToken = "8268119707:AAF_jZsbhoChaJf-VZaKETaFnb9T9V1dc-g"
$providerToken = "1744374395:TEST:e622a5a9c69996bd9809"
$botUsername = "iktrainingbot"
$apiUrl = "http://localhost:5004"

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

$ngrokArgs = if ([string]::IsNullOrWhiteSpace($ReservedUrl)) {
    "http 5004 --config `"$NgrokConfig`""
}
else {
    "http 5004 --url $ReservedUrl --config `"$NgrokConfig`""
}

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "$NgrokCommand $ngrokArgs"
)

Write-Host "PowerFitness API, bot and ngrok tunnel are starting." -ForegroundColor Green
Write-Host "ngrok config: $NgrokConfig" -ForegroundColor DarkGray
Write-Host "Copy the HTTPS ngrok URL and paste it into the app on the 'Адрес сервера' card." -ForegroundColor Cyan
