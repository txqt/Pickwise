# Pickwise Design System

Pickwise is a local desktop companion for League of Legends. The UI should feel like a practical control panel: clear state, explicit player actions, visible diagnostics, and no hidden automation.

## Principles

- Player control first: every write action is a visible button click.
- State over decoration: status, summoner, ready check, champion select, and command result must be easy to scan.
- League-inspired, not League-cloned: dark champion-selection surfaces are acceptable, but do not copy Riot's client chrome pixel-for-pixel.
- Failure-tolerant visuals: missing champion icons, CDN failures, and cache failures must never block LCU detection or player commands.
- Local-first: no backend, no stored Riot credentials, and no required Riot Web API call for the current MVP.

## Layout

- Window uses a single vertical scroll column with 12px rounded cards.
- Light cards hold connection, lobby, ready check, and readiness info.
- Champion Select uses a dark card because icon grids scan better on a dark surface.
- Keep primary actions close to the state they affect: lobby creation under mode selection, accept/decline under ready check, pick/ban under champion grid.

## Components

- Status header: app name and current connection/gameflow status.
- Current Summoner card: shows the signed-in summoner or `Not connected`.
- Mode Selection card: queue dropdown and `Create Lobby`.
- Ready Check card: ready-check state and explicit `Accept` / `Decline`.
- Champion Select card: search box, wrapped champion icon grid, selected tile, explicit `Pick` / `Ban`.
- Readiness card: diagnostic log path, champion icon cache path, and Riot disclaimer.
- Tray icon: keeps Pickwise reachable while hidden, with `Show Pickwise` and `Exit` menu items where platform support is available.
- Ready-check alert: on transition into ready check, show the main window and change title/tray tooltip to `Pickwise - Match Found`.

## Visual Tokens

- Light card background: `#f5f5f5`
- Champion Select background: `#071316`
- Champion grid background: `#0b1d22`
- Champion tile background: `#10242a`
- Champion border: `#2e5661`
- Champion primary text: `#d6e7e7`
- Champion muted text: `#8ba3a3`
- General muted text: `#666`

## Do / Don't

Do:

- Keep controls boring and obvious.
- Keep error text visible in the main window.
- Keep logs discoverable.
- Use Data Dragon icons as an enhancement only.
- Hide to tray on window close; quit only through the tray `Exit` command.

Don't:

- Add auto-accept, auto-pick, auto-ban, or auto-matchmaking.
- Hide write actions behind background jobs.
- Make the app look like an official Riot client.
- Require icon downloads for core app behavior.
- Spam alerts every polling tick; alert only on state transition into ready check.
