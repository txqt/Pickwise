# LCU Compliance Notes

Track every League Client endpoint before public release.

| Flow | Endpoint | Read/Write | Trigger | Purpose | Risk note |
|---|---|---:|---|---|---|
| Client discovery | `LeagueClientUx` process + `lockfile`, or `--app-port` + `--remoting-auth-token` process args | Read | Automatic | Find the local League Client connection details. | Local-only discovery; do not persist credentials. |
| Current Summoner | `GET /lol-summoner/v1/current-summoner` | Read | Automatic | Show the currently signed-in summoner. | Read-only; avoid storing summoner identity as an account system. |
| Lobby creation | `POST /lol-lobby/v2/lobby` | Write | Player Command | Create the selected League lobby mode. | Write endpoint; must be disclosed to Riot. Creates lobby only; does not start matchmaking. |
| Matchmaking start | `POST /lol-lobby/v2/lobby/matchmaking/search` | Write | Player Command | Start matchmaking for the current lobby only after the player clicks Find Match. | Write endpoint; must be disclosed to Riot. No automatic matchmaking start. |
| Matchmaking cancel | `DELETE /lol-lobby/v2/lobby/matchmaking/search` | Write | Player Command | Cancel the current lobby matchmaking search after the player clicks Cancel Search. | Write endpoint; must be disclosed to Riot. |
| Ready Check observe | `GET /lol-matchmaking/v1/ready-check`, or `OnJsonApiEvent` for `/lol-matchmaking/v1/ready-check` | Read | Automatic | Detect when a match needs player response. | Read-only observation. |
| Ready Check command | `POST /lol-matchmaking/v1/ready-check/accept`, `POST /lol-matchmaking/v1/ready-check/decline` | Write | Player Command | Accept or decline when the player clicks in the app. | Write endpoint; must be disclosed to Riot. No auto accept. |
| Champion Select observe | `GET /lol-champ-select/v1/session`, or `OnJsonApiEvent` for `/lol-champ-select/v1/session` | Read | Automatic | Show current champion select state. | Read-only observation. |
| Champion Select command | `PATCH /lol-champ-select/v1/session/actions/{id}` | Write | Player Command | Pick or ban only after explicit player click. | Write endpoint; must be disclosed to Riot. No auto pick/ban. |

Temporary queues such as ARAM Mayhem (`queueId 2400`) may be unavailable outside Riot's event/region rollout. Pickwise should surface the LCU error instead of silently falling back to another queue.
