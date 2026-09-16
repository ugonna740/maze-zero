import { randomBytes, randomUUID } from 'node:crypto';
import { createServer } from 'node:http';
import { WebSocket, WebSocketServer } from 'ws';
import { issueAuthToken, verifyAuthToken, verifyNimiqLogin, type LoginRequest } from './auth.js';
import { Match } from './game.js';
import type { ClientMessage } from './protocol.js';
import { verifyMainnetStake } from './stakes.js';

const port = Number(process.env.PORT || 8787);
const duration = Number(process.env.MATCH_SECONDS || 180);
const treasury = process.env.TREASURY_ADDRESS || '';
const stakesEnabled = false; // Do not accept real-money matches before payout/refund flows are implemented.
const matches = new Map<string, Match>();
const sockets = new Map<string, { ws: WebSocket; matchId?: string; address?: string }>();
const origin = process.env.PUBLIC_ORIGIN || '';
if (process.env.NODE_ENV === 'production' && (!origin || !process.env.AUTH_SECRET || !process.env.PUBLIC_WS_URL)) throw new Error('PUBLIC_ORIGIN, PUBLIC_WS_URL and AUTH_SECRET are required in production.');
const httpServer = createServer(async (request, response) => {
  const requestOrigin = request.headers.origin || '';
  if (origin && requestOrigin === origin) response.setHeader('access-control-allow-origin', origin);
  response.setHeader('vary', 'Origin'); response.setHeader('content-type', 'application/json');
  if (request.method === 'OPTIONS') { response.writeHead(requestOrigin === origin ? 204 : 403); response.end(); return; }
  if (request.method === 'GET' && request.url === '/health') { response.writeHead(200); response.end(JSON.stringify({ ok: true, network: 'main' })); return; }
  if (request.method === 'POST' && request.url === '/api/auth/nimiq') {
    try {
      if (origin && requestOrigin !== origin) throw new Error('Request origin is not allowed.');
      let raw = ''; for await (const chunk of request) { raw += chunk; if (raw.length > 16_384) throw new Error('Request too large.'); }
      const identity = verifyNimiqLogin(JSON.parse(raw) as LoginRequest, origin);
      response.writeHead(200); response.end(JSON.stringify({ authToken: issueAuthToken(identity), address: identity.address, serverUrl: process.env.PUBLIC_WS_URL || '' }));
    } catch (error) { response.writeHead(401); response.end(JSON.stringify({ error: error instanceof Error ? error.message : 'Authorization failed.' })); }
    return;
  }
  response.writeHead(404); response.end(JSON.stringify({ error: 'Not found.' }));
});
const server = new WebSocketServer({ server: httpServer });

const send = (ws: WebSocket, value: object) => ws.readyState === WebSocket.OPEN && ws.send(JSON.stringify(value));
const broadcast = (match: Match) => { for (const [id, client] of sockets) if (client.matchId === match.state.id) send(client.ws, { type: 'state', selfId: id, state: match.state }); };
const code = () => randomBytes(3).toString('hex').toUpperCase();

function join(clientId: string, match: Match, address: string) {
  match.addPlayer(clientId, address); sockets.get(clientId)!.matchId = match.state.id; broadcast(match);
}

server.on('connection', ws => {
  const clientId = randomUUID(); sockets.set(clientId, { ws });
  send(ws, { type: 'connected', clientId, network: 'main' });

  ws.on('message', async raw => {
    try {
      const message = JSON.parse(raw.toString()) as ClientMessage;
      const client = sockets.get(clientId)!;
      if (message.type === 'join_public' || message.type === 'create_private' || message.type === 'join_private') {
        if (client.matchId) throw new Error('Already joined.');
        if (message.type !== 'join_public' && !stakesEnabled) throw new Error('Private stake matches are not available.');
        const identity = verifyAuthToken(message.authToken); client.address = identity.address;
        if (message.type === 'join_public') {
          let match = [...matches.values()].find(m => m.state.kind === 'public' && m.state.phase === 'lobby' && m.state.players.length < 4);
          if (!match) { match = new Match('public'); matches.set(match.state.id, match); }
          join(clientId, match, identity.address);
        } else if (message.type === 'create_private') {
          if (!Number.isSafeInteger(message.stakeLuna) || message.stakeLuna <= 0) throw new Error('Stake must be a positive Luna amount.');
          const match = new Match('private-stake', message.stakeLuna); match.state.code = code(); matches.set(match.state.id, match); join(clientId, match, identity.address);
        } else {
          const match = [...matches.values()].find(m => m.state.code === message.code.toUpperCase());
          if (!match) throw new Error('Private match not found.'); join(clientId, match, identity.address);
        }
        return;
      }
      if (!client.matchId) throw new Error('Join a match first.');
      const match = matches.get(client.matchId); if (!match) throw new Error('Match ended.');
      if (message.type === 'ready') match.setReady(clientId);
      if (message.type === 'move') match.move(clientId, message.position, message.yaw, message.sequence);
      if (message.type === 'collect') match.collect(clientId, message.orbId);
      if (message.type === 'deposit') match.deposit(clientId);
      if (message.type === 'confirm_stake') {
        if (!stakesEnabled) throw new Error('Private stake matches are not available.');
        if (!treasury || !client.address) throw new Error('Stake treasury is not configured.');
        await verifyMainnetStake({ hash: message.transactionHash, sender: client.address, recipient: treasury, value: match.state.stakeLuna, matchId: match.state.id });
        match.confirmStake(clientId);
      }
      broadcast(match);
    } catch (error) { send(ws, { type: 'error', message: error instanceof Error ? error.message : 'Invalid request.' }); }
  });

  ws.on('close', () => {
    const client = sockets.get(clientId); const match = client?.matchId ? matches.get(client.matchId) : undefined;
    if (match) { match.remove(clientId); broadcast(match); } sockets.delete(clientId);
  });
});

setInterval(() => { for (const match of matches.values()) { match.tick(Date.now(), duration); broadcast(match); } }, 100);
httpServer.listen(port, () => console.log(`Maze Zero authoritative multiplayer listening on :${port} (Nimiq mainnet)`));
