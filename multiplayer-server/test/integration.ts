import { createHmac } from 'node:crypto';
import { spawn } from 'node:child_process';
import { WebSocket } from 'ws';
import { Match } from '../src/game.js';
import { BufferUtils, Hash, KeyPair } from '@nimiq/core';

const port = 18787;
const secret = 'maze-zero-integration-secret';
const url = `ws://127.0.0.1:${port}`;

function token(address: string): string {
  const body = Buffer.from(JSON.stringify({ address, network: 'main', exp: Math.floor(Date.now() / 1000) + 120 })).toString('base64url');
  return `${body}.${createHmac('sha256', secret).update(body).digest('base64url')}`;
}

class TestClient {
  readonly ws = new WebSocket(url);
  messages: any[] = [];
  constructor(readonly address: string) { this.ws.on('message', raw => this.messages.push(JSON.parse(raw.toString()))); }
  async open() { if (this.ws.readyState !== WebSocket.OPEN) await new Promise<void>((resolve, reject) => { this.ws.once('open', resolve); this.ws.once('error', reject); }); }
  send(value: object) { this.ws.send(JSON.stringify(value)); }
  async wait(predicate: (message: any) => boolean, timeout = 8000): Promise<any> {
    const started = Date.now();
    while (Date.now() - started < timeout) {
      const found = [...this.messages].reverse().find(predicate); if (found) return found;
      await new Promise(resolve => setTimeout(resolve, 25));
    }
    throw new Error(`Timed out. Last message: ${JSON.stringify(this.messages.at(-1))}`);
  }
  close() { this.ws.close(); }
}

async function move(client: TestClient, from: any, to: any, sequence: { value: number }) {
  const length = Math.hypot(to.x - from.x, to.z - from.z); const steps = Math.max(1, Math.ceil(length / .55));
  for (let i = 1; i <= steps; i++) {
    const t = i / steps; client.send({ type: 'move', position: { x: from.x + (to.x - from.x) * t, y: 0, z: from.z + (to.z - from.z) * t }, yaw: 0, sequence: ++sequence.value });
    await new Promise(resolve => setTimeout(resolve, 105));
  }
}

const testOrigin = 'https://maze-zero.test';
const server = spawn(process.execPath, ['dist/server.js'], { cwd: process.cwd(), env: { ...process.env, PORT: String(port), AUTH_SECRET: secret, PUBLIC_ORIGIN: testOrigin, PUBLIC_WS_URL: url, MATCH_SECONDS: '30', TRAP_COUNT: '0' }, stdio: ['ignore', 'pipe', 'pipe'] });
try {
  await new Promise<void>((resolve, reject) => { server.stdout.on('data', data => data.toString().includes('listening') && resolve()); server.once('exit', code => reject(new Error(`Server exited ${code}`))); setTimeout(() => reject(new Error('Server start timeout')), 5000); });
  const wallet = KeyPair.generate(); const challenge = `Maze Zero login\nMode: multiplayer\nOrigin: ${testOrigin}\nNonce: 123e4567-e89b-42d3-a456-426614174000\nIssued: ${new Date().toISOString()}`;
  const signedHash = Hash.computeSha256(BufferUtils.fromUtf8(`\x16 Nimiq Signed Message:\n${challenge.length}${challenge}`));
  const loginBody = { address: wallet.toAddress().toUserFriendlyAddress(), challenge, signature: JSON.stringify({ publicKey: wallet.publicKey.toHex(), signature: wallet.sign(signedHash).toHex() }), deviceId: 'integration-device' };
  const login = await fetch(`http://127.0.0.1:${port}/api/auth/nimiq`, { method: 'POST', headers: { 'content-type': 'application/json', origin: testOrigin }, body: JSON.stringify(loginBody) });
  if (!login.ok) throw new Error(`Signed Nimiq login failed: ${await login.text()}`); const verifiedLogin = await login.json() as { authToken: string; serverUrl: string };
  const replay = await fetch(`http://127.0.0.1:${port}/api/auth/nimiq`, { method: 'POST', headers: { 'content-type': 'application/json', origin: testOrigin }, body: JSON.stringify(loginBody) });
  if (replay.status !== 401) throw new Error('Replayed Nimiq challenge was accepted.');
  const a = new TestClient(loginBody.address); const b = new TestClient('NQ00 TEST PLAYER B'); await Promise.all([a.open(), b.open()]);
  a.send({ type: 'join_public', authToken: verifiedLogin.authToken }); b.send({ type: 'join_public', authToken: token(b.address) });
  const joined = await a.wait(m => m.type === 'state' && m.state.players.length === 2); a.send({ type: 'ready' }); b.send({ type: 'ready' });
  const playing = await a.wait(m => m.type === 'state' && m.state.phase === 'playing', 6000);
  const self = playing.state.players.find((p: any) => p.id === playing.selfId); const orb = playing.state.orbs.find((o: any) => o.available); const sequence = { value: 0 };
  await move(a, self.position, orb.position, sequence); a.send({ type: 'collect', orbId: orb.id });
  const carrying = await a.wait(m => m.type === 'state' && m.state.players.find((p: any) => p.id === m.selfId)?.carried === 1);
  await move(a, carrying.state.players.find((p: any) => p.id === carrying.selfId).position, carrying.state.bank, sequence); a.send({ type: 'deposit' });
  await a.wait(m => m.type === 'state' && m.state.players.find((p: any) => p.id === m.selfId)?.banked === 1);
  const finished = await a.wait(m => m.type === 'state' && m.state.phase === 'finished', 36000);
  if (finished.state.winnerId !== finished.selfId) throw new Error('The player who banked the orb was not declared winner.');
  a.close(); b.close();

  const privateClient = new TestClient('NQ00 PRIVATE PLAYER'); await privateClient.open();
  privateClient.send({ type: 'create_private', authToken: token(privateClient.address), stakeLuna: 100000 });
  await privateClient.wait(m => m.type === 'error' && m.message.includes('not available'));
  privateClient.close();
  delete process.env.TRAP_COUNT;
  const lethal = new Match('public'); const victim = lethal.addPlayer('victim', 'NQ00 VICTIM'); const survivor = lethal.addPlayer('survivor', 'NQ00 SURVIVOR');
  victim.carried = 3; lethal.state.traps = [{ id: 'certain-trap', position: victim.position, radius: 2 }]; lethal.setReady(victim.id); lethal.setReady(survivor.id); lethal.tick(Date.now() + 4000, 30); lethal.tick(Date.now() + 4100, 30);
  if (victim.alive || victim.carried !== 0) throw new Error('Trap elimination did not enforce one life and carried-orb loss.');
  const contested = new Match('public'); const one = contested.addPlayer('one', 'NQ ONE'); const two = contested.addPlayer('two', 'NQ TWO'); const sharedOrb = contested.state.orbs[0];
  one.position = { ...sharedOrb.position }; two.position = { ...sharedOrb.position }; contested.state.phase = 'playing'; contested.state.endsAt = Date.now() + 30000;
  contested.collect(one.id, sharedOrb.id); contested.collect(two.id, sharedOrb.id);
  if (one.carried + two.carried !== 1) throw new Error('A contested orb was awarded more than once.');
  const bank = contested.state.bank;
  if (contested.state.traps.some(t => Math.hypot(t.position.x - bank.x, t.position.z - bank.z) < 2)) throw new Error('A trap was generated on the bank.');
  if (!verifiedLogin.authToken || verifiedLogin.serverUrl !== url) throw new Error('Authorization response did not include deployment routing.');
  console.log('PASS: signed Nimiq auth, replay rejection, two-player match, bank winner, one-life death, and disabled stake');
} finally {
  server.kill();
}
