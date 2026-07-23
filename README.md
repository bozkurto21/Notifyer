# Notifyer

Windows system-tray app that reminds you to rest your eyes on an interval. Stays quiet while games (or any process you list) are running; Film Mode pushes the next reminder far out.

## Features

- Editable interval (default 20 min)
- Windows toast with **selectable system sounds** (Default, Reminder, Mail, Alarms, Calls, …)
- **Bypass Do Not Disturb** (urgent toast) so reminders can still appear during game focus / DND
- **Quiet Apps:** no toast while listed processes are running; fires as soon as they exit
- **Film Mode:** next reminder in ~3 hours, then back to normal
- **Phone check:** independent daily toasts at 12:00 / 14:00 / 16:00 (ignores pause, film mode, quiet apps)
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
dotnet publish -c Release -r win-x64 --self-contained true -o ..\..
```

## Config example

```json
{
  "intervalMinutes": 20,
  "sound": "reminder",
  "bypassDoNotDisturb": true,
  "message": "Camdan dışarı bak, gözlerini dinlendir.",
  "filmModeMinutes": 180,
  "quietProcesses": ["cs2.exe", "csgo.exe", "EscapeFromTarkov.exe"],
  "startWithWindows": true,
  "phoneCheckEnabled": true,
  "phoneCheckTimes": ["12:00", "14:00", "16:00"],
  "phoneCheckMessage": "Telefonuna bak — önemli bir haber kaçırmış olabilirsin.",
  "phoneCheckSound": "reminder",
  "phoneCheckBypassDoNotDisturb": true
}
```

`sound` values: `off`, `default`, `im`, `mail`, `reminder`, `sms`, `alarm`…`alarm10`, `call`…`call10`.

Tray menu: **Ses**, **Do Not Disturb’ı aş**, **Telefon kontrolü (12/14/16)**, **Test bildirimi**.

Phone-check toasts always fire on schedule (when enabled) — they do not respect pause, Film Mode, or quiet apps.

If a game still swallows toasts, add the game to `quietProcesses` (defer until exit) or check Windows **Settings → System → Notifications** priority list for Notifyer.

Add a game to `quietProcesses`, then use **Reload config** from the tray menu.
