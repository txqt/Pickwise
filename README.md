# Pickwise

Pickwise is a local Windows desktop companion for League of Legends. It connects to the running League Client through the local LCU API and lets the player perform explicit, user-clicked actions from a desktop window.

## Current MVP

- Detects the local League Client.
- Shows the current summoner.
- Creates Normal Draft 5v5, ARAM, or ARAM Mayhem lobbies from explicit player clicks.
- Shows Ready Check state.
- Lets the player click Accept or Decline.
- Shows Champion Select state.
- Lets the player filter by role, search a champion, and click Pick or Ban.
- Shows cached Data Dragon champion icons when available; missing icons do not block app actions.
- Stays available from the system tray where supported; closing the window hides it, tray Exit quits it.
- Writes local diagnostic and crash logs.

Pickwise does not auto-accept, auto-pick, auto-ban, run an overlay, store Riot credentials, or use a backend.

## Requirements

- Windows
- .NET 8 SDK
- League of Legends client

## Setup

Copy the example env file and add your Riot API key:

```powershell
Copy-Item .env.example .env
```

The current MVP uses the local League Client API. `RIOT_API_KEY` is reserved for later Riot Web API features such as match history.

## Run

```powershell
dotnet run --project src\Pickwise\Pickwise.csproj
```

You can start Pickwise before or after opening the League Client. It waits until `LeagueClientUx` is running.

## Checks

```powershell
dotnet build Pickwise.sln -v minimal --no-restore
dotnet run --project tests\Pickwise.Tests\Pickwise.Tests.csproj --no-build
```

## Manual League Client smoke test

1. Start Pickwise before or after opening League Client.
2. Confirm Pickwise shows the current summoner after League Client is logged in.
3. Create a lobby from Mode Selection.
4. Queue manually in League Client.
5. Use Pickwise to Accept or Decline when ready check appears.
6. In champion select, filter by role, search a champion, and click Pick or Ban.
7. Disconnect internet and restart Pickwise to confirm missing champion icons show placeholders without blocking the app.
8. Close the Pickwise window and confirm it stays in the tray; use tray `Show Pickwise` and `Exit`.

## Publish test build

```powershell
dotnet publish src\Pickwise\Pickwise.csproj -c Release -r win-x64 --self-contained false
```

Run the output from:

```text
src\Pickwise\bin\Release\net8.0\win-x64\publish
```

## Package portable release

```powershell
.\scripts\package-release.ps1
```

The script creates:

```text
artifacts\Pickwise-win-x64.zip
```

This is a framework-dependent build. The target machine needs the .NET 8 Desktop Runtime installed. To run it, unzip the archive and start `Pickwise.exe`.

## Download release

Public test builds are available on the GitHub Releases page:

```text
https://github.com/txqt/Pickwise/releases
```

## Logs

```text
%LOCALAPPDATA%\Pickwise\diagnostic.log
%LOCALAPPDATA%\Pickwise\crash.log
```

Champion icons are cached under:

```text
%LOCALAPPDATA%\Pickwise\champion-icons
```

## Known limitations

- Windows is the only tested target.
- Champion icons are best-effort Data Dragon assets; missing icons show placeholders.
- ARAM Mayhem is a temporary queue and may be unavailable outside Riot's rollout.
- Riot Web API features such as match history are not implemented yet.
- No installer is included yet; use the publish folder for test builds.
- No Windows toast or sound alert yet; ready-check alert uses the app window/title and tray tooltip where tray is available.

## Riot compliance notes

All write actions are explicit player commands. Pickwise does not make gameplay decisions for the player. See [docs/LCU-COMPLIANCE.md](docs/LCU-COMPLIANCE.md) for the current endpoint inventory.

Pickwise is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.

Use [docs/RIOT-REGISTRATION.md](docs/RIOT-REGISTRATION.md) when filling the Riot Developer Portal app registration.
