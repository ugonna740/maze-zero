# Maze Zero authoritative multiplayer

Real-time 2–4 player server for public matchmaking and private NIM-stake rooms.

## Rules enforced by the server

- One life; disconnect or trap elimination has no respawn.
- Orbs are globally owned and can be collected only once.
- Carried orbs do not score until deposited inside the bank radius.
- Death removes carried orbs; already-banked score remains.
- Highest banked score wins when time expires, all players die, or every orb is banked.

## Production requirements

Set `AUTH_SECRET`, `TREASURY_ADDRESS`, and `CHAIN_VERIFY_URL`. Authorization tokens must be issued only after backend verification of the Nimiq signed challenge. The chain verifier must check a confirmed Nimiq **mainnet** transaction's sender, recipient, Luna value and match ID data. The server fails closed when these are missing.

Run `npm install`, `npm run build`, then `npm start`.
