# Riot Registration Draft

Use this as the source text when registering Pickwise or describing its Riot integration.

## Short description

Pickwise is a local Windows desktop companion for League of Legends. It connects to the running League Client through the local LCU API and lets the player perform explicit, user-clicked actions such as creating a selected lobby, accepting or declining ready check, and selecting a champion during champion select.

## Player value

Pickwise provides a compact desktop control panel for common League Client flows. It shows the current summoner, selected queue mode, ready-check state, champion-select state, and a searchable champion grid with Data Dragon icons.

## Current feature list

- Detect the local running League Client.
- Show the currently signed-in summoner.
- Create a player-selected lobby for supported queues.
- Start or cancel matchmaking for the current lobby only after the player clicks the button.
- Show ready-check state.
- Accept or decline ready check only after the player clicks the button.
- Show champion-select state.
- Pick or ban a champion only after the player selects a champion and clicks the button.
- Cache Data Dragon champion icons locally as a non-blocking visual enhancement.
- Write local diagnostic and crash logs.

## Explicit non-goals

- No auto-accept.
- No auto-pick or auto-ban.
- No automatic matchmaking start; matchmaking starts only after clicking Find Match.
- No gameplay automation.
- No memory reading.
- No backend account system.
- No stored Riot credentials.
- No win-rate, MMR, hidden-player, or game-session-specific advantage features.

## Riot / LCU endpoint disclosure

See [LCU-COMPLIANCE.md](LCU-COMPLIANCE.md) for the endpoint inventory. All write endpoints are triggered only by explicit player commands.

## Visible legal disclaimer

Pickwise is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.
