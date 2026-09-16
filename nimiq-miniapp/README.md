# Maze Zero — Nimiq Mini App shell

Mobile-first WebGL host for Maze Zero. Practice is wallet-free; Daily Duel and NIM Arena request a Nimiq account, signed challenge and privacy-preserving device identifier.

## Run

1. Run `npm install`.
2. Copy a Unity WebGL build into `public/unity` (or configure the loader/build URLs in `.env`).
3. Run `npm run dev`.

For production, set `VITE_API_BASE_URL`. The server must verify signed challenges, issue deterministic run seeds, validate submitted results and own leaderboard/prize authority. The client fallback is only for UI testing.

Arena recipient and entry amount remain unset in `.env.example`; no transaction is offered until event economics and treasury ownership are approved.
