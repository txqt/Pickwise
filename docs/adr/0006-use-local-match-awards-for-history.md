# Use local match awards for history

Match Detail will show Pickwise-derived Match Awards instead of waiting for an official Riot MVP/SVP field. MVP is the highest-scored participant in the match, SVP is the second highest-scored participant, and the remaining participants are shown as Top 3, Top 4, and so on. Match History rows do not show these titles; the player opens detail to inspect the full ranking.

The v1 scorer is a transparent local heuristic from completed match participant stats: kills, assists, damage, gold, creep score, and largest multikill increase score, while deaths reduce score. If LCU does not provide enough participant or team data, Pickwise shows no ranked detail button for that match instead of guessing.

LCU match list payloads may include only the viewed participant, so Pickwise reads `lol-match-history/v1/games/{gameId}` before ranking when the list row lacks full participant data.
