# Pickwise MVP Plan

## Scope

- Windows MVP.
- Avalonia/.NET Companion Shell.
- Local MVP: no backend, no app account.
- Desktop Companion Window with a single State View.
- System tray and Ready Check Alert.
- League Client discovery and LCU connection.
- Current Summoner detection.
- Ready Check detection with explicit Accept/Decline Player Commands.
- Champion Select Session detection with explicit Pick/Ban Player Commands.
- Local Diagnostic Log.
- Endpoint compliance notes before public release.

## Deferred

- In-game overlay.
- Auto accept, auto pick, auto ban.
- Meta, build, rune, and counter-pick features.
- Match History and stats.
- Backend, cloud sync, telemetry.
- Multi-game support.

## Implementation order

1. Avalonia shell: single State View + tray.
2. League Client discovery: process/lockfile.
3. LCU HTTP client.
4. Current Summoner read.
5. Ready Check observe + Accept/Decline Player Command.
6. Champion Select observe.
7. Pick/Ban Player Command.
8. Local Diagnostic Log.
9. Endpoint compliance doc.
