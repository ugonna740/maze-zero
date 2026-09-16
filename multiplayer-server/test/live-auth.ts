import { randomUUID } from 'node:crypto';
import { BufferUtils, Hash, KeyPair } from '@nimiq/core';
import { WebSocket } from 'ws';

const apiUrl = process.env.MAZE_ZERO_API_URL;
const origin = process.env.MAZE_ZERO_ORIGIN;
if (!apiUrl || !origin) throw new Error('Set MAZE_ZERO_API_URL and MAZE_ZERO_ORIGIN.');

const wallet = KeyPair.generate();
const challenge = `Maze Zero login\nMode: multiplayer\nOrigin: ${origin}\nNonce: ${randomUUID()}\nIssued: ${new Date().toISOString()}`;
const signedHash = Hash.computeSha256(BufferUtils.fromUtf8(`\x16 Nimiq Signed Message:\n${challenge.length}${challenge}`));
const response = await fetch(new URL('/api/auth/nimiq', apiUrl), {
  method: 'POST',
  headers: { 'content-type': 'application/json', origin },
  body: JSON.stringify({
    address: wallet.toAddress().toUserFriendlyAddress(),
    challenge,
    signature: { publicKey: wallet.publicKey.toHex(), signature: wallet.sign(signedHash).toHex() },
    deviceId: 'live-smoke-test',
  }),
});
if (!response.ok) throw new Error(`Live Nimiq auth failed (${response.status}): ${await response.text()}`);
const result = await response.json() as { authToken?: string; serverUrl?: string };
if (!result.authToken || !result.serverUrl?.startsWith('wss://')) throw new Error('Live auth did not return secure match routing.');

const ws = new WebSocket(result.serverUrl, { origin });
try {
  await new Promise<void>((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error('Live WebSocket timed out.')), 10000);
    ws.once('message', raw => {
      clearTimeout(timeout);
      const message = JSON.parse(raw.toString()) as { type?: string; network?: string };
      if (message.type !== 'connected' || message.network !== 'main') reject(new Error('Unexpected live WebSocket greeting.'));
      else resolve();
    });
    ws.once('error', reject);
  });
  console.log('PASS: live Nimiq-signed auth and secure WebSocket connection');
} finally {
  ws.close();
}
