# Pickwise

A desktop companion app for League of Legends players that reacts to the local League Client state.

## Language

**Pickwise**:
A desktop app that observes the local League Client and helps the player before or during a League of Legends match.
_Avoid_: Blitz clone, multi-game hub, DraftMate

**Player Command**:
An explicit action the player triggers inside the companion app that is then applied to the League Client.
_Avoid_: automation, bot action, auto-pick, auto-accept

**Windows MVP**:
The first usable version of the companion app, scoped to Windows players only.
_Avoid_: cross-platform MVP

**Companion Shell**:
The desktop application surface the player uses to view League Client state and issue Player Commands.
_Avoid_: web dashboard, overlay shell

**Desktop Companion Window**:
The main non-overlay window where the player views League Client state and issues Player Commands.
_Avoid_: in-game overlay

**Champion Select Session**:
The pre-game phase where players ban and choose champions after matchmaking has succeeded.
_Avoid_: lobby, queue pop, loading screen

**Ready Check**:
The match-found prompt where the player accepts or declines entering Champion Select.
_Avoid_: auto accept, matchmaking success

**Current Summoner**:
The League of Legends account currently signed in through the local League Client.
_Avoid_: stored account, Riot login, managed account

**Client Connection**:
The companion app's live connection to the local League Client while it is running.
_Avoid_: client launcher, Riot login session

**Local MVP**:
The first usable version that runs entirely on the player's machine without a companion backend or app account.
_Avoid_: cloud account, hosted control plane

**Local Preference**:
A non-sensitive setting stored on the player's machine to preserve the Companion Shell experience.
_Avoid_: Riot credential, session token, stored summoner identity

**State View**:
A single Companion Shell view that changes its content based on the current League Client state.
_Avoid_: dashboard, multi-page navigation

**Ready Check Alert**:
A native desktop notification that tells the player a Ready Check needs a response.
_Avoid_: custom notification system, in-game alert

**Local Diagnostic Log**:
A file on the player's machine that records connection, state, and Player Command outcomes for troubleshooting.
_Avoid_: telemetry, remote error reporting

**Match History**:
The player's recent completed League of Legends matches and basic performance details.
_Avoid_: live match state, meta dashboard
