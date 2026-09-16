# Maze Zero

Maze Zero is a mobile-first Nimiq Pay Mini App. Practice mode is wallet-free. Public Multiplayer uses Nimiq-signed identity to place two to four players in one live maze. Players have one life, collect orbs, and deposit them at the central bank. The most banked orbs wins; there is no multiplayer escape or respawn.

## Project layout

- `nimiq-miniapp/` — Nimiq Pay web wrapper and Unity WebGL player.
- `multiplayer-server/` — authoritative match state and Nimiq signature verification.
- `unity-source/` — original Unity C# scripts, WebGL plugins, scene YAML and configuration. Third-party source art is not included.

## Current release scope

Public Multiplayer and Practice are the intended launch modes. Private NIM stake matches and NIM Arena are disabled in the public UI until mainnet deposits, refunds, payout operations and legal review are complete. The server deliberately rejects unverified stake confirmations.

## Local development

Use Unity 6000.0.61f1 to open the full art-bearing local project, or import the `unity-source` code into a Unity 6 project with the required art assets. Build WebGL into `nimiq-miniapp/public/unity`. Run `npm ci && npm run build` in both TypeScript projects. Run `npm run test:integration` in `multiplayer-server` for the local two-player protocol test.

The web build requires `VITE_API_BASE_URL` at build time. The server requires `AUTH_SECRET`, `PUBLIC_ORIGIN`, and `PUBLIC_WS_URL` in production. See each `.env.example`. Never commit private keys, wallet recovery phrases or `.env` files.

## Third-party assets

The playable WebGL build includes user-supplied art and textures. Unity source art is excluded from this repository because its individual redistribution terms need review. The Chequered Ink Cartoon UI Pack license is supplied in the local Unity project; Fredoka is under the SIL Open Font License. The Kenney UI Pack is CC0. Review and document the Obstacle Pack, character model, and downloaded texture licenses before distributing their raw source assets.

See [DEPLOYMENT.md](DEPLOYMENT.md) for Railway, Cloudflare Pages, and launch smoke tests.

## License

Original Maze Zero source code is MIT licensed; third-party assets retain their respective licenses.
