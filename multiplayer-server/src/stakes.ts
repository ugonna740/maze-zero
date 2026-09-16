export async function verifyMainnetStake(input: { hash: string; sender: string; recipient: string; value: number; matchId: string }): Promise<void> {
  const endpoint = process.env.CHAIN_VERIFY_URL;
  if (!endpoint) throw new Error('Mainnet stake verification is not configured.');
  const response = await fetch(endpoint, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ ...input, network: 'main', minConfirmations: 1 }) });
  if (!response.ok) throw new Error('Stake transaction is not confirmed.');
  const result = await response.json() as { valid?: boolean };
  if (!result.valid) throw new Error('Stake transaction does not match this player and match.');
}
