import { init, requestDeviceIdentifier } from '@nimiq/mini-app-sdk';
import type { GameMode, LaunchPayload } from './unity-bridge';

type Provider = Awaited<ReturnType<typeof init>>;
let provider: Provider | null = null;

export async function authorizeMode(mode: Exclude<GameMode, 'practice'>): Promise<LaunchPayload> {
  provider ??= await init();
  const accounts = await provider.listAccounts();
  if (!Array.isArray(accounts)) throw new Error(accounts.error?.message || 'Could not read Nimiq accounts.');
  const account = accounts[0];
  if (!account) throw new Error('Choose a Nimiq account to continue.');

  const nonce = crypto.randomUUID();
  const challenge = [
    'Maze Zero login',
    `Mode: ${mode}`,
    `Origin: ${location.origin}`,
    `Nonce: ${nonce}`,
    `Issued: ${new Date().toISOString()}`,
  ].join('\n');
  const signed = await provider.sign(challenge);
  if ('error' in signed) throw new Error(signed.error?.message || 'Signature was declined.');
  const deviceId = await requestDeviceIdentifier({
    reason: 'Help prevent duplicate match accounts and protect fair multiplayer rankings.',
  });

  const payload: LaunchPayload = {
    mode,
    address: account,
    deviceId,
    signature: JSON.stringify(signed),
    challenge,
    runId: nonce,
  };

  const api = import.meta.env.VITE_API_BASE_URL;
  if (!api) throw new Error('Multiplayer is not configured yet. Practice mode is still available.');
  const response = await fetch(`${api}/api/auth/nimiq`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!response.ok) throw new Error('The run server rejected wallet authorization.');
  const session = await response.json() as { authToken?: string; serverUrl?: string };
  if (!session.authToken || !session.serverUrl?.startsWith('wss://')) throw new Error('The multiplayer server is not ready.');
  return { ...payload, ...session };
}

export function shortAddress(address?: string): string {
  return address ? `${address.slice(0, 7)}…${address.slice(-5)}` : 'Connect Nimiq';
}
