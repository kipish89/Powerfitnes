# Android Export

## Current status

The project is prepared for Android export:

- target framework: `net10.0-android`
- app name: `PowerFitness`
- app id: `com.powerfitness.mobile`
- internet/cleartext permissions are already enabled for local API work

## Quick test build from Visual Studio

1. Open `C:\Users\User\Documents\New project\PowerFitness.slnx`
2. Select project `PowerFitness.App`
3. Select target `Android Emulator` or connected Android device
4. Start in `Debug`

## Export APK from PowerShell

Test APK without custom signing:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\New project\Export-Android.ps1" -PackageFormat apk -Configuration Release
```

The output folder will be:

`C:\Users\User\Documents\New project\PowerFitness.App\bin\Release\net10.0-android\publish`

## Export signed APK or AAB

Example:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\New project\Export-Android.ps1" `
  -PackageFormat aab `
  -Configuration Release `
  -UseKeystore `
  -KeystoreFile "C:\keys\powerfitness.keystore" `
  -KeystorePassword "your-store-password" `
  -KeyAlias "powerfitness" `
  -KeyPassword "your-key-password"
```

## Keystore creation

Example command:

```powershell
keytool -genkeypair -v -keystore "C:\keys\powerfitness.keystore" -alias powerfitness -keyalg RSA -keysize 2048 -validity 10000
```

## Notes

- `apk` is convenient for direct installation on a phone.
- `aab` is the preferred format for Google Play.
- If publish fails because NuGet cannot be reached, restore/build again when internet access to NuGet is available.
- For universal Android testing through your computer, use a public API URL such as an ngrok HTTPS tunnel and save it inside the app.
