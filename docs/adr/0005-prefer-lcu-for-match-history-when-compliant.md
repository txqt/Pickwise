# Prefer LCU for match history when compliant

Post-MVP Match History will use League Client data first when the endpoint is available, sufficient, and acceptable for Riot compliance. The Riot Web API remains the fallback because it is the official public path but brings API keys, rate limits, and product registration overhead.
