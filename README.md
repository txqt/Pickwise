# Pickwise

Pickwise is a local Windows desktop companion for League of Legends. It connects to the running League Client through the local LCU API and lets the player perform explicit, user-clicked actions from a desktop window.

## Current MVP

- Detects the local League Client.
- Shows the current summoner.
- Creates Normal Draft 5v5, ARAM, or ARAM Mayhem lobbies from explicit player clicks.
- Shows Ready Check state.
- Lets the player click Accept or Decline.
- Shows Champion Select state.
- Lets the player search a champion and click Pick or Ban.
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

## Logs

```text
%LOCALAPPDATA%\Pickwise\diagnostic.log
%LOCALAPPDATA%\Pickwise\crash.log
```

## Riot compliance notes

All write actions are explicit player commands. Pickwise does not make gameplay decisions for the player. See [docs/LCU-COMPLIANCE.md](docs/LCU-COMPLIANCE.md) for the current endpoint inventory.
