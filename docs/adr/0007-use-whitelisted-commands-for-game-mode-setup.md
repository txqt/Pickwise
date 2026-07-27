# Use whitelisted commands for Game Mode Setup

Pickwise renders Game Mode Setup from typed capability descriptors, but those descriptors may only reference whitelisted Player Commands already implemented in the app. This keeps the setup UI adaptable to different lobby capabilities without allowing schema data to invent new LCU writes outside the compliance list.
