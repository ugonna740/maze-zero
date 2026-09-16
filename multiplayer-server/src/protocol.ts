export type MatchKind = 'public' | 'private-stake';
export type Phase = 'lobby' | 'awaiting-stakes' | 'countdown' | 'playing' | 'finished';
export type Vec3 = { x: number; y: number; z: number };

export type ClientMessage =
  | { type: 'join_public'; authToken: string }
  | { type: 'create_private'; authToken: string; stakeLuna: number }
  | { type: 'join_private'; authToken: string; code: string }
  | { type: 'confirm_stake'; transactionHash: string }
  | { type: 'ready' }
  | { type: 'move'; position: Vec3; yaw: number; sequence: number }
  | { type: 'collect'; orbId: string }
  | { type: 'deposit' };

export interface PlayerState {
  id: string; address: string; position: Vec3; yaw: number; alive: boolean;
  carried: number; banked: number; ready: boolean; stakeConfirmed: boolean;
  lastDepositAt: number; sequence: number;
}
export interface OrbState { id: string; position: Vec3; available: boolean }
export interface TrapState { id: string; position: Vec3; radius: number }
export interface MatchState {
  id: string; code?: string; kind: MatchKind; phase: Phase; seed: number;
  mazeWidth: number; mazeHeight: number; cellSize: number;
  stakeLuna: number; endsAt: number; bank: Vec3; players: PlayerState[];
  orbs: OrbState[]; traps: TrapState[]; winnerId?: string; finishReason?: string;
}
