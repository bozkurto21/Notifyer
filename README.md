# Notifyer

Windows system-tray app that reminds you to rest your eyes on an interval. Stays quiet while games (or any process you list) are running; Film Mode pushes the next reminder far out.

## Features

- Editable interval (default 20 min)
- Windows toast notification (+ sound on/off)
- **Quiet Apps:** no toast while listed processes are running; fires as soon as they exit
- **Film Mode:** next reminder in ~3 hours, then back to normal
- Start with Windows
- Config: `%AppData%\Notifyer\config.json`

## Download

Grab the latest `Notifyer.exe` from [Releases](../../releases). Run it once — a tray icon appears. No installer needed.

## Build from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
cd Notifyer
dotnet run
```

Self-contained single exe:

```powershell
cd Notifyer
dotnet publish -c Release -r win-x64 --self-contained true -o ..\publish
```

## Config example

```json
{
  "intervalMinutes": 20,
  "soundEnabled": true,
  "message": "Camdan dışarı bak, gözlerini dinlendir.",
  "filmModeMinutes": 180,
  "quietProcesses": ["cs2.exe", "csgo.exe"],
  "startWithWindows": true
}
```

Add a game to `quietProcesses`, then use **Reload config** from the tray menu.
