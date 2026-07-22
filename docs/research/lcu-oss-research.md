# LCU OSS research for Windows Avalonia MVP

Date: 2026-07-22  
Scope: League Client discovery via process/lockfile, Current Summoner read, Ready Check observe + Accept/Decline, Champion Select observe + Pick/Ban.  
Target: .NET/Avalonia Windows desktop app, player-clicked commands only.

## Short recommendation

Use a small .NET LCU adapter. First try `Kunc.RiotGames.Lol.LeagueClientUpdate`; if it adds friction, copy/adapt only the lockfile parsing and HTTP/WebSocket patterns from MIT sources. Do not copy automation behavior from auto-accept/instalock apps.

Minimum endpoint set to track in compliance docs:

| Flow | Endpoint/event | Mode |
|---|---|---|
| Discovery | `LeagueClientUx` process, `lockfile`, or `--app-port` + `--remoting-auth-token` | Local discovery |
| Current Summoner | `GET /lol-summoner/v1/current-summoner` | Read |
| Ready Check observe | `GET /lol-matchmaking/v1/ready-check` or WebSocket `OnJsonApiEvent` for that URI; fallback: `/lol-gameflow/v1/gameflow-phase` | Read |
| Ready Check command | `POST /lol-matchmaking/v1/ready-check/accept`, `POST /lol-matchmaking/v1/ready-check/decline` | Player-clicked write |
| Champion Select observe | `GET /lol-champ-select/v1/session` or WebSocket `OnJsonApiEvent` for that URI | Read |
| Champion Select command | `PATCH /lol-champ-select/v1/session/actions/{id}` with `championId` and optionally `completed: true` | Player-clicked write |

## Riot policy/compliance baseline

- Player-serving products must be registered with Riot even if they do not use official documented APIs. Source: Riot League docs, Registration section: <https://developer.riotgames.com/docs/lol#registration>
- Riot's 2019 LCU policy change says apps using League Client API must contact Riot before release and use only approved endpoints. Source: <https://www.riotgames.com/en/DevRel/changes-to-the-lcu-api-policy>
- LCU and in-game APIs are expected to keep working with Vanguard, but Riot Developer Relations does not own or support them and does not guarantee updates. Source: Riot FAQ: <https://developer.riotgames.com/docs/faqs#will-the-lcu-be-impacted-by-vanguard>
- Game integrity risk: apps must not create unfair advantage, remove game decisions, identify hidden players, or dictate player decisions. Source: Riot League docs, Game Integrity and unapproved use cases: <https://developer.riotgames.com/docs/lol#game-integrity>
- Dev keys and API usage can be rate-limited; this matters later for Web API fallback, less for local LCU. Source: Riot API Terms, Limitations on Usage: <https://developer.riotgames.com/terms#limitations-on-usage>

Compliance stance for MVP: keep all write actions explicitly player-clicked, keep an endpoint inventory, do not support Korea unless Riot approval says otherwise, and disclose every LCU endpoint before public release.

## .NET candidates

### 1. Kunc.RiotGames

- Repo: <https://github.com/AoshiW/Kunc.RiotGames>
- Package: <https://www.nuget.org/packages/Kunc.RiotGames.Lol.LeagueClientUpdate>
- License: MIT.
- Maintenance signal: active; GitHub API showed pushed `2026-07-15`, not archived; NuGet package `0.17.0` is current in search results.
- Coverage:
  - Discovery: `FileOverProcessLockfileProvider` finds `LeagueClientUx`, derives `lockfile` path, watches it, and parses it. Source: <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/FileOverProcessLockfileProvider.cs#L41-L92>
  - Discovery alternative: `ProcessArgsLockfileProvider` reads process command line and extracts `--app-port` / `--remoting-auth-token`. Source: <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/ProcessArgsLockfileProvider.cs#L92-L104>
  - Auth/HTTP: `Lockfile` creates auth header/credential; `LolLeagueClientUpdate` sets `https://127.0.0.1:{port}/` and Basic auth, accepting the local self-signed cert. Sources: <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/Lockfile.cs#L155-L172>, <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/LolLeagueClientUpdate.cs#L36-L83>
  - Current Summoner: README shows `GetFromJsonAsync(... "lol-summoner/v1/current-summoner")`. Source: <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/README.md#L24-L27>
  - Observe events: `Subscribe` APIs and WebSocket `OnJsonApiEvent` subscription. Source: <https://github.com/AoshiW/Kunc.RiotGames/blob/dev/src/Kunc.RiotGames.Lol.LeagueClientUpdate/LolLeagueClientUpdate.WebSocket.cs#L33-L47>
  - Ready Check / Champ Select commands: generic HTTP supports these endpoints, not typed wrappers.
- Copy/adapt safety: safe to use as dependency or adapt small pieces under MIT. Prefer dependency first; copy only if API shape fights Avalonia app simplicity.
- Policy concerns: library is neutral; our endpoint usage still needs Riot disclosure. Write endpoints remain Player Command only.

### 2. Briar / GrrrLCU

- Repo: <https://github.com/BlossomiShymae/GrrrLCU>
- License: MIT.
- Maintenance signal: pushed `2025-07-05`, not archived; small wrapper.
- Coverage:
  - Discovery: `ProcessFinder` locates `LeagueClientUx`. Source: <https://github.com/BlossomiShymae/GrrrLCU/blob/main/BlossomiShymae.Briar/Utils/ProcessFinder.cs#L16-L34>
  - Lockfile credentials: `PortTokenWithLockfile` reads and splits `lockfile`. Source: <https://github.com/BlossomiShymae/GrrrLCU/blob/main/BlossomiShymae.Briar/Utils/Behaviors/PortTokenWithLockfile.cs#L22-L49>
  - Process args credentials: `PortTokenWithProcessList` extracts `--remoting-auth-token` and `--app-port`. Source: <https://github.com/BlossomiShymae/GrrrLCU/blob/main/BlossomiShymae.Briar/Utils/Behaviors/PortTokenWithProcessList.cs#L23-L66>
  - Current Summoner: README/test examples use `GET /lol-summoner/v1/current-summoner`. Source: <https://github.com/BlossomiShymae/GrrrLCU/blob/main/README.md>
  - Observe/commands: generic HTTP/WebSocket wrapper; not typed to ready-check/champ-select.
- Copy/adapt safety: safe under MIT. Good reference for fallback discovery behavior, but less active than Kunc.
- Policy concerns: neutral wrapper; same endpoint disclosure requirement.

### 3. lcu-sharp

- Repo: <https://github.com/bryanhitc/lcu-sharp>
- License: MIT.
- Maintenance signal: archived; pushed `2022-07-17`; 44 stars.
- Coverage:
  - Discovery: `LeagueProcessHandler` waits for `LeagueClientUx`, then `LockFileHandler` reads the lockfile. Sources: <https://github.com/bryanhitc/lcu-sharp/blob/master/LCUSharp/Utility/LeagueProcessHandler.cs#L51-L81>, <https://github.com/bryanhitc/lcu-sharp/blob/master/LCUSharp/Utility/LockFileHandler.cs#L21-L35>
  - HTTP: `LeagueClientApi` builds request handler and tests `/riotclient/app-name`. Source: <https://github.com/bryanhitc/lcu-sharp/blob/master/LCUSharp/LeagueClientApi.cs#L90-L107>
  - Events: README shows `api.EventHandler.Subscribe("/lol-gameflow/v1/gameflow-phase", ...)`. Source: <https://github.com/bryanhitc/lcu-sharp#event-example>
  - Current Summoner: generic requests; README example targets summoner icon, not read.
  - Ready Check / Champ Select: generic HTTP only.
- Copy/adapt safety: safe under MIT, but use only as reference. Archived code should not be the main dependency.
- Policy concerns: neutral wrapper.

### 4. PoniLCU

- Repo: <https://github.com/Ponita0/PoniLCU>
- License: MIT.
- Maintenance signal: last pushed `2022-05-30`; not archived but stale.
- Coverage:
  - Discovery: supports command-line credentials and lockfile. Source: <https://github.com/Ponita0/PoniLCU/blob/master/PoniLCU/LeagueClient.cs#L330-L390>
  - HTTP: generic `Request(method, url, body)` builds local HTTPS requests. Source: <https://github.com/Ponita0/PoniLCU/blob/master/PoniLCU/LeagueClient.cs#L110-L157>
  - Events: WebSocket subscription map. Source: <https://github.com/Ponita0/PoniLCU/blob/master/PoniLCU/LeagueClient.cs#L281-L308>
  - Current Summoner: README example uses `GET /lol-summoner/v1/current-summoner`. Source: <https://github.com/Ponita0/PoniLCU/blob/master/README.md#L71-L85>
  - Ready Check / Champ Select: generic HTTP only.
- Copy/adapt safety: safe under MIT, but stale and style is rough. Reference only.
- Policy concerns: neutral wrapper.

### 5. LeagueAutoAccept

- Repo: <https://github.com/sweetriverfish/LeagueAutoAccept>
- License: MIT.
- Maintenance signal: active; pushed `2026-07-08`; 118 stars.
- Coverage:
  - Discovery: `LCU.cs` finds `LeagueClientUx`, reads WMI command line, extracts `--app-port` and `--remoting-auth-token`. Source: <https://github.com/sweetriverfish/LeagueAutoAccept/blob/main/Leauge%20Auto%20Accept/LCU.cs#L94-L123>
  - Current Summoner: `Data.cs` calls `GET lol-summoner/v1/current-summoner`. Source: <https://github.com/sweetriverfish/LeagueAutoAccept/blob/main/Leauge%20Auto%20Accept/Data.cs#L42>
  - Ready Check: `MainLogic.cs` calls `POST lol-matchmaking/v1/ready-check/accept` and `/decline`. Source: <https://github.com/sweetriverfish/LeagueAutoAccept/blob/main/Leauge%20Auto%20Accept/MainLogic.cs#L135-L147>
  - Champion Select observe: `GET lol-champ-select/v1/session`. Source: <https://github.com/sweetriverfish/LeagueAutoAccept/blob/main/Leauge%20Auto%20Accept/MainLogic.cs#L175>
  - Pick/Ban: patches `lol-champ-select/v1/session/actions/{actId}` with `championId` and with `completed = true`. Source: <https://github.com/sweetriverfish/LeagueAutoAccept/blob/main/Leauge%20Auto%20Accept/MainLogic.cs#L588-L616>
- Copy/adapt safety: license allows small-copy, but copy endpoint shapes only. Do not copy automation loop, instalock behavior, chat messaging, rune/spell writes, or queue restart features.
- Policy concerns: high if copied as-is. README explicitly describes automatic queue accept / pick / ban and calls it a gray area. Source: <https://github.com/sweetriverfish/LeagueAutoAccept#features>

## Other-language OSS references

### lcu-driver

- Repo: <https://github.com/sousa-andre/lcu-driver>
- License: MIT.
- Maintenance signal: pushed `2024-11-30`; 122 stars.
- Useful pieces:
  - Process/lockfile connection model in `connection.py`. Source: <https://github.com/sousa-andre/lcu-driver/blob/master/lcu_driver/connection.py#L21-L52>
  - WebSocket subscription to `OnJsonApiEvent`. Source: <https://github.com/sousa-andre/lcu-driver/blob/master/lcu_driver/connection.py#L178>
  - Current Summoner HTTP example. Source: <https://github.com/sousa-andre/lcu-driver/blob/master/examples/tutorial/http_request.py>
- Copy/adapt safety: use as conceptual reference only; Python code is not worth porting.
- Policy concerns: neutral library.

### hasagi-core

- Repo: <https://github.com/dysolix/hasagi-core>
- License: MIT.
- Maintenance signal: active; pushed `2026-07-15`; tests/docs are current.
- Useful pieces:
  - Credential parsing handles process and lockfile paths/content. Source: <https://github.com/dysolix/hasagi-core/blob/main/src/util.ts>
  - Tests cover lockfile edge cases and avoid leaking password in errors. Source: <https://github.com/dysolix/hasagi-core/blob/main/tests/unit/util.test.ts>
  - Event docs show path-based subscription for `/lol-champ-select/v1/session`. Source: <https://github.com/dysolix/hasagi-core/blob/main/docs/Events.md#listening-by-lcu-path>
  - Generated event types include `OnJsonApiEvent_lol-summoner_v1_current-summoner`. Source: <https://github.com/dysolix/hasagi-core/blob/main/src/types/lcu-events.d.ts#L816>
- Copy/adapt safety: use as reference for edge cases and event API shape; do not port TypeScript wholesale.
- Policy concerns: neutral library.

### Rift Explorer / LCU Explorer

- Repos: <https://github.com/Pupix/rift-explorer>, <https://github.com/HextechDocs/lcu-explorer>
- Licenses: Rift Explorer MIT; LCU Explorer has no license detected by GitHub API, so do not copy code from LCU Explorer.
- Maintenance signal: Rift Explorer pushed `2023-03-03`; LCU Explorer archived, pushed `2024-08-10`.
- Useful pieces:
  - Endpoint exploration / schema discovery, not app runtime code.
  - Rift Explorer watches and parses `lockfile`. Source: <https://github.com/Pupix/rift-explorer/blob/master/app/util/RiotConnector.ts#L148-L184>
- Copy/adapt safety: Rift Explorer MIT but Electron/TS; reference only. LCU Explorer: no license detected, do not copy.
- Policy concerns: endpoint explorers can expose write endpoints; our app must still register and disclose actual usage.

## Endpoint schema notes

Riot does not provide stable official LCU endpoint documentation in the same way as Web API docs. Riot-linked community docs state that the LCU must be running and logged in, and recommend creating a dev portal application explaining LCU usage. Source: <https://riot-api-libraries.readthedocs.io/en/latest/lcu.html>

Community-generated schema pages list the scoped endpoints:

- Ready Check: `GET /lol-matchmaking/v1/ready-check`, `POST /lol-matchmaking/v1/ready-check/accept`, `POST /lol-matchmaking/v1/ready-check/decline`. Source: <https://lcu.vivide.re/>
- Champion Select: `GET /lol-champ-select/v1/session`, `PATCH /lol-champ-select/v1/session/my-selection`, and related champ-select endpoints. Source: <https://lcu.kebs.dev/>

Use source code references above as the stronger evidence for concrete request bodies:

- Ready Check accept/decline: no body in LeagueAutoAccept.
- Pick/Ban action: `PATCH /lol-champ-select/v1/session/actions/{actId}` with `{ championId }`; lock-in/ban completion uses `{ completed: true, championId }`.

## What to copy

Copy/adapt only these small patterns if not using a dependency:

1. Lockfile parse format: `LeagueClient:<pid>:<port>:<password>:https`.
2. Windows process discovery for `LeagueClientUx`.
3. Basic auth header from `riot:{password}`.
4. Local HTTPS client with self-signed cert handling for `https://127.0.0.1:{port}/`.
5. WebSocket subscription to `OnJsonApiEvent`.
6. Endpoint request shapes for player-clicked ready-check and champ-select actions.

Skipped: auto accept, auto pick, auto ban, chat messages, lobby restart, rune/spell writes, memory reading, overlays.
