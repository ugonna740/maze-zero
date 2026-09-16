# Maze Zero — Cycle II submission draft

Live Mini App: https://maze-zero.pages.dev/

Source: https://github.com/ugonna740/maze-zero

Team lead pseudonym / GitHub: ugonna740 / https://github.com/ugonna740

Public prize-payout address supplied by the team lead: `NQ20 G6CQ UV4T X5SH UPLJ G87X 2V68 3CP2 KGYL`

## Description (under 250 words)

Maze Zero is a mobile-first, third-person maze runner built for short, competitive sessions. Practice mode starts instantly without a wallet. In public multiplayer, two to four players enter a procedurally generated maze, collect orbs, and deposit them at the central bank. Each player has one life; there are no escapes or respawns. The winner is the player who has banked the most orbs when the match ends.

Nimiq Pay is part of the multiplayer identity flow: players choose a Nimiq account and approve a signed challenge in their wallet. The server verifies the signature and issues a short-lived match session; private keys never enter the game. The Unity WebGL client runs inside the Mini App, with touch controls, a minimap, a compass, traps, and a live match HUD.

Private NIM stakes and NIM Arena are visibly disabled while mainnet deposit, refund, and payout handling remain under review. The current public mode is free to play and never asks players to send NIM.

## Before submitting

- Complete a two-device public match inside Nimiq Pay, including movement, orb deposit, death, and final ranking.
- Load `https://maze-zero.pages.dev/` through Nimiq Pay's Custom URL test field. A catalog deep link does not resolve until the app is listed.
- Confirm the submission form accepts the supplied NQ payout address; competition FAQ also describes USDT prize payments on Polygon.
- Confirm rights and attribution for the character, Obstacle Pack, and downloaded textures included in the compiled Unity build.
- Add an optional short demo video or walkthrough if time permits.
