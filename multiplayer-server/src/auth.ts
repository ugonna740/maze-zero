import { createHmac, timingSafeEqual } from 'node:crypto';
import { BufferUtils, Hash, PublicKey, Signature } from '@nimiq/core';

export interface Identity { address: string; exp: number; network: 'main' }
const usedNonces = new Map<string, number>();

export interface LoginRequest { address: string; challenge: string; signature: string | { publicKey: string; signature: string }; deviceId?: string }

export function verifyNimiqLogin(input: LoginRequest, expectedOrigin: string): Identity {
  if (!input.address || !input.challenge || !input.signature) throw new Error('Incomplete Nimiq authorization.');
  const lines = Object.fromEntries(input.challenge.split('\n').slice(1).map(line => { const split = line.indexOf(':'); return split < 0 ? ['', ''] : [line.slice(0, split).trim(), line.slice(split + 1).trim()]; }));
  if (!input.challenge.startsWith('Maze Zero login\n') || !['multiplayer', 'nim-arena'].includes(lines.Mode)) throw new Error('Wrong authorization purpose.');
  if (expectedOrigin && lines.Origin !== expectedOrigin) throw new Error('Authorization origin mismatch.');
  const issued = Date.parse(lines.Issued); const now = Date.now();
  if (!Number.isFinite(issued) || Math.abs(now - issued) > 5 * 60_000) throw new Error('Authorization challenge expired.');
  if (!/^[0-9a-f-]{36}$/i.test(lines.Nonce || '') || usedNonces.has(lines.Nonce)) throw new Error('Authorization nonce is invalid or already used.');
  const signed = typeof input.signature === 'string' ? JSON.parse(input.signature) as { publicKey: string; signature: string } : input.signature;
  const publicKey = PublicKey.fromHex(signed.publicKey); const signature = Signature.fromHex(signed.signature);
  const address = publicKey.toAddress().toUserFriendlyAddress();
  const normalize = (value: string) => value.replace(/\s/g, '').toUpperCase();
  if (normalize(address) !== normalize(input.address)) throw new Error('Public key does not match the selected Nimiq address.');
  const prefix = '\x16 Nimiq Signed Message:\n';
  const hash = Hash.computeSha256(BufferUtils.fromUtf8(prefix + input.challenge.length + input.challenge));
  if (!publicKey.verify(signature, hash)) throw new Error('Nimiq signature is invalid.');
  usedNonces.set(lines.Nonce, now + 10 * 60_000);
  for (const [nonce, expiry] of usedNonces) if (expiry < now) usedNonces.delete(nonce);
  return { address, network: 'main', exp: Math.floor(now / 1000) + 15 * 60 };
}

export function issueAuthToken(identity: Identity): string {
  const secret = process.env.AUTH_SECRET; if (!secret) throw new Error('AUTH_SECRET is required.');
  const body = Buffer.from(JSON.stringify(identity)).toString('base64url');
  return `${body}.${createHmac('sha256', secret).update(body).digest('base64url')}`;
}

export function verifyAuthToken(token: string): Identity {
  const secret = process.env.AUTH_SECRET;
  if (!secret) throw new Error('AUTH_SECRET is required.');
  const [body, signature] = token.split('.');
  if (!body || !signature) throw new Error('Malformed authorization token.');
  const expected = createHmac('sha256', secret).update(body).digest('base64url');
  const a = Buffer.from(signature); const b = Buffer.from(expected);
  if (a.length !== b.length || !timingSafeEqual(a, b)) throw new Error('Invalid authorization token.');
  const identity = JSON.parse(Buffer.from(body, 'base64url').toString('utf8')) as Identity;
  if (identity.network !== 'main' || identity.exp < Date.now() / 1000 || !identity.address) throw new Error('Authorization expired or not mainnet.');
  return identity;
}
