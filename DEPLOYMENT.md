# Maze Zero deployment

The launch scope is Practice and free Public Multiplayer. Do not enable private stakes or NIM Arena until on-chain deposit, refund, payout, dispute, and legal flows are implemented and audited. The public Nimiq prize-payout address is not a match treasury.

## Railway — multiplayer server

Create a service from this repository with root directory `multiplayer-server`. Railway should build its `Dockerfile`. Generate a public HTTPS domain, then set:

- `AUTH_SECRET`: a unique, randomly generated secret (at least 32 bytes). Set only in Railway, never in Git.
- `PUBLIC_ORIGIN`: the exact Cloudflare Pages origin, with no trailing slash.
- `PUBLIC_WS_URL`: the Railway domain with `wss://` and no path.
- `NIMIQ_NETWORK=main`
- `MATCH_SECONDS=180`, `MIN_PLAYERS=2`, `MAX_PLAYERS=4` (or leave defaults).

Leave `TREASURY_ADDRESS` and `NIMIQ_RPC_URL` unset while stakes are disabled. Confirm `https://<railway-domain>/health` returns `{"ok":true,"network":"main"}`. Railway provides `PORT`; the server reads it automatically.

## Cloudflare Pages — Nimiq Mini App

Connect this repository to Pages, set root directory `nimiq-miniapp`, build command `npm ci && npm run build`, and output directory `dist`. Set build variable `VITE_API_BASE_URL=https://<railway-domain>`. The Unity WebGL player must be present in `nimiq-miniapp/public/unity/Build` before building. Vite copies it to `dist/unity/Build`.

If a custom domain is added, update `PUBLIC_ORIGIN` on Railway to the exact new origin and redeploy. The app must be served over HTTPS for the wallet and WebSocket flows. Check the three `.unityweb` files' HTTP content encoding against the compression format used by Unity; configure Cloudflare Pages `_headers` only for the actual format.

## Smoke test

1. Open the Pages URL on a desktop browser and a phone. Confirm the Unity player loads, Practice starts without wallet access, and the joystick responds to touch.
2. Open within Nimiq Pay, connect two distinct Nimiq accounts, and join Public Multiplayer from two devices. Confirm both enter one match and see synchronized movement.
3. Collect and deposit orbs. Confirm the player with the larger deposited total wins, and a dead player does not respawn.
4. Confirm private stakes and NIM Arena are visibly disabled. Do not send real NIM to test them.

## Submission

Use public payout address `NQ20 G6CQ UV4T X5SH UPLJ G87X 2V68 3CP2 KGYL` only in the competition entry's prize-payout field. Never submit a recovery phrase, private key, or Railway `AUTH_SECRET`.
