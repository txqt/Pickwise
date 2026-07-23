# LCU Compliance Notes

Track every League Client endpoint before public release.

| Flow | Endpoint | Read/Write | Trigger | Purpose | Risk note |
|---|---|---:|---|---|---|
| Client discovery | `LeagueClientUx` process + `lockfile`, or `--app-port` + `--remoting-auth-token` process args | Read | Automatic | Find the local League Client connection details. | Local-only discovery; do not persist credentials. |
| Current Summoner | `GET /lol-summoner/v1/current-summoner` | Read | Automatic | Show the currently signed-in summoner. | Read-only; avoid storing summoner identity as an account system. |
| Queue catalog | `GET /lol-game-queues/v1/queues` | Read | Automatic | Load visible, enabled game modes and mode capability flags such as lane selector and quickplay slot setup. | Read-only; hidden/disabled queues should not be offered as normal choices. |
| Lobby creation | `POST /lol-lobby/v2/lobby` | Write | Player Command | Create the selected League lobby mode. | Write endpoint; must be disclosed to Riot. Creates lobby only; does not start matchmaking. |
| Lobby leave | `DELETE /lol-lobby/v2/lobby` | Write | Player Command | Leave the current lobby after the player clicks Leave Lobby. | Write endpoint; must be disclosed to Riot. No automatic lobby leave. |
| Lobby lane preferences | `PUT /lol-lobby/v2/lobby/members/localMember/position-preferences` | Write | Player Command | Save the player's primary and secondary lane preferences for queues where LCU exposes `showPositionSelector`. | Write endpoint; must be disclosed to Riot. No automatic lane changes. |
| Quickplay slot recommendations | `GET /lol-perks/v1/quick-play-selections/champion/{championId}/position/{position}` | Read | Player Command follow-up | Fetch Riot's recommended runes for a selected Quickplay champion/lane before saving slots. | Read-only; used only after the player saves Quickplay slot edits. |
| Quickplay slot update | `PUT /lol-lobby/v1/lobby/members/localMember/player-slots` | Write | Player Command | Save the local player's two Quickplay slots with champion, lane, skin, summoner spells, and recommended runes. | Write endpoint; must be disclosed to Riot. No automatic Quickplay slot changes. |
| Matchmaking start | `POST /lol-lobby/v2/lobby/matchmaking/search` | Write | Player Command | Start matchmaking for the current lobby only after the player clicks Find Match. | Write endpoint; must be disclosed to Riot. No automatic matchmaking start. |
| Matchmaking cancel | `DELETE /lol-lobby/v2/lobby/matchmaking/search` | Write | Player Command | Cancel the current lobby matchmaking search after the player clicks Cancel Search. | Write endpoint; must be disclosed to Riot. |
| Ready Check observe | `GET /lol-matchmaking/v1/ready-check`, or `OnJsonApiEvent` for `/lol-matchmaking/v1/ready-check` | Read | Automatic | Detect when a match needs player response. | Read-only observation. |
| Ready Check command | `POST /lol-matchmaking/v1/ready-check/accept`, `POST /lol-matchmaking/v1/ready-check/decline` | Write | Player Command | Accept or decline when the player clicks in the app. | Write endpoint; must be disclosed to Riot. No auto accept. |
| Champion Select observe | `GET /lol-champ-select/v1/session`, or `OnJsonApiEvent` for `/lol-champ-select/v1/session` | Read | Automatic | Show current champion select state, including ARAM bench from `benchChampions[*].championId` or legacy `benchChampionIds`. | Read-only observation. |
| Champion Select mode detect | `GET /lol-gameflow/v1/session` | Read | Automatic | Detect queue/game mode so Champion Select can show 5v5 or ARAM controls. | Read-only; fallback should avoid enabling ARAM writes if mode is unknown. |
| Champion Select availability | `GET /lol-champ-select/v1/pickable-champion-ids`, `GET /lol-champ-select/v1/disabled-champion-ids`, `GET /lol-lobby-team-builder/champ-select/v1/subset-champion-list` | Read | Automatic | Limit champion lists to currently valid choices and ARAM card pools. | Read-only; missing data should hide unavailable picks rather than issue guesses. |
| Champion Select command | `PATCH /lol-champ-select/v1/session/actions/{id}` | Write | Player Command | Declare on champion avatar click with `{ championId }`; pick or ban only after explicit button click with `{ championId, completed: true }`. | Write endpoint; must be disclosed to Riot. No auto pick/ban. |
| ARAM Champion Select command | `POST /lol-lobby-team-builder/champ-select/v1/session/bench/swap/{championId}`, `POST /lol-champ-select/v1/session/bench/swap/{championId}` | Write | Player Command | Swap to a bench champion only after explicit player click. | Write endpoints; must be disclosed to Riot. No auto bench swap. |

Temporary queues such as ARAM Mayhem (`queueId 2400`) may be unavailable outside Riot's event/region rollout. Pickwise should surface the LCU error instead of silently falling back to another queue.
