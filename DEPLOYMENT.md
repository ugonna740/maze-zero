# Maze Zero deployment

The launch scope is Practice and free Public Multiplayer. Do not enable private stakes or NIM Arena until on-chain deposit, refund, payout, dispute, and legal flows are implemented and audited. The public Nimiq prize-payout address is not a match treasury.

## Current production deployment (2026-09-16)

- Game: https://maze-zero.pages.dev/
- Multiplayer API: https://maze-zero-server-production.up.railway.app/
- Server health: https://maze-zero-server-production.up.railway.app/health
- GitHub: https://github.com/ugonna740/maze-zero

The first release was deployed with the Railway and Wrangler CLIs. GitHub pushes do not yet trigger automatic hosting deployments. The public site loaded the Unity player and started Practice in a browser; the API passed live Nimiq-signed authentication and `wss://` connection tests. A two-device public match inside Nimiq Pay still needs human acceptance testing.

## Railway — multiplayer server

The project is `maze-zero` and the service is `maze-zero-server`. Its first deployment was uploaded from `multiplayer-server`, and Railway built its `Dockerfile`. Set:

- `AUTH_SECRET`: a unique, randomly generated secret (at least 32 bytes). Set only in Railway, never in Git.
- `PUBLIC_ORIGIN`: the exact Cloudflare Pages origin, with no trailing slash.
- `PUBLIC_WS_URL`: the Railway domain with `wss://` and no path.
- `NIMIQ_NETWORK=main`
- `MATCH_SECONDS=180`, `MIN_PLAYERS=2`, `MAX_PLAYERS=4` (or leave defaults).

Leave `TREASURY_ADDRESS` and `NIMIQ_RPC_URL` unset while stakes are disabled. Confirm the health URL above returns `{"ok":true,"network":"main"}`. Railway provides `PORT`; the server reads it automatically. To publish server changes from the local checkout, run `railway up --service maze-zero-server` from `multiplayer-server`.

## Cloudflare Pages — Nimiq Mini App

The Pages project is `maze-zero`. To publish web changes from `nimiq-miniapp`, set `VITE_API_BASE_URL=https://maze-zero-server-production.up.railway.app` for `npm run build`, then run `wrangler pages deploy dist --project-name maze-zero --branch main`. The Unity WebGL player must be present in `nimiq-miniapp/public/unity/Build` before building. Vite copies it to `dist/unity/Build`.

If a custom domain is added, update `PUBLIC_ORIGIN` on Railway to the exact new origin and redeploy. The app must be served over HTTPS for the wallet and WebSocket flows. Check the three `.unityweb` files' HTTP content encoding against the compression format used by Unity; configure Cloudflare Pages `_headers` only for the actual format.

## Smoke test

1. Open the Pages URL on a desktop browser and a phone. Confirm the Unity player loads, Practice starts without wallet access, and the joystick responds to touch.
2. Open within Nimiq Pay, connect two distinct Nimiq accounts, and join Public Multiplayer from two devices. Confirm both enter one match and see synchronized movement. This is the outstanding release acceptance test.
3. Collect and deposit orbs. Confirm the player with the larger deposited total wins, and a dead player does not respawn.
4. Confirm private stakes and NIM Arena are visibly disabled. Do not send real NIM to test them.

## Submission

Use public payout address `NQ20 G6CQ UV4T X5SH UPLJ G87X 2V68 3CP2 KGYL` only in the competition entry's prize-payout field. Never submit a recovery phrase, private key, or Railway `AUTH_SECRET`.
