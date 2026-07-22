# Ship the LCU control loop before meta features

The MVP will focus on connecting to the local League Client, observing Current Summoner, Ready Check, and Champion Select Session state, and applying explicit Player Commands. Meta, build, rune, and counter-pick features are deferred because they require separate data sources, patch-version handling, and cache rules that do not prove the desktop LCU flow.
