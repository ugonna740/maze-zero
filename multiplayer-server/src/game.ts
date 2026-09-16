import { randomInt, randomUUID } from 'node:crypto';
import type { MatchKind, MatchState, PlayerState, TrapState, Vec3 } from './protocol.js';
import { generateMazeObjects } from './maze.js';

const distance = (a: Vec3, b: Vec3) => Math.hypot(a.x - b.x, a.z - b.z);
const cellPoint = (seed: number, i: number, width: number, height: number, size: number): Vec3 => ({ x: Math.abs(seed * 17 + i * 29) % width * size, y: 0, z: Math.abs(seed * 31 + i * 19) % height * size });

export class Match {
  readonly state: MatchState;
  private lastTick = Date.now();

  constructor(kind: MatchKind, stakeLuna = 0) {
    const seed = randomInt(100000, 999999);
    const mazeWidth = 12, mazeHeight = 12, cellSize = 4;
    const maze = generateMazeObjects(mazeWidth, mazeHeight, cellSize, seed);
    const trapCount = Number(process.env.TRAP_COUNT ?? 10);
    const reserved = new Set([`0,0`, `${mazeWidth - 1},${mazeHeight - 1}`, `0,${mazeHeight - 1}`, `${mazeWidth - 1},0`, `${Math.floor(mazeWidth / 2)},${Math.floor(mazeHeight / 2)}`]);
    const traps: TrapState[] = []; const used = new Set<string>();
    for (let i = 0; traps.length < trapCount && i < 200; i++) {
      const position = cellPoint(seed + 37, i, mazeWidth, mazeHeight, cellSize); const key = `${position.x / cellSize},${position.z / cellSize}`;
      if (reserved.has(key) || used.has(key)) continue; used.add(key); traps.push({ id: `t${traps.length}`, position, radius: 1.15 });
    }
    this.state = { id: randomUUID(), kind, phase: kind === 'private-stake' ? 'awaiting-stakes' : 'lobby', seed, mazeWidth, mazeHeight, cellSize, stakeLuna, endsAt: 0, bank: maze.bank, players: [], orbs: maze.orbs, traps };
  }

  addPlayer(id: string, address: string): PlayerState {
    if (this.state.phase === 'playing' || this.state.players.length >= 4) throw new Error('Match is not joinable.');
    const corners: Vec3[] = [{ x: 0, y: 0, z: 0 }, { x: (this.state.mazeWidth - 1) * this.state.cellSize, y: 0, z: (this.state.mazeHeight - 1) * this.state.cellSize }, { x: 0, y: 0, z: (this.state.mazeHeight - 1) * this.state.cellSize }, { x: (this.state.mazeWidth - 1) * this.state.cellSize, y: 0, z: 0 }];
    const p: PlayerState = { id, address, position: corners[this.state.players.length], yaw: 0, alive: true, carried: 0, banked: 0, ready: false, stakeConfirmed: this.state.kind === 'public', lastDepositAt: 0, sequence: 0 };
    this.state.players.push(p); return p;
  }

  setReady(id: string): void {
    this.player(id).ready = true;
    if (this.state.players.length >= 2 && this.state.players.every(p => p.ready && p.stakeConfirmed)) {
      this.state.phase = 'countdown'; this.state.endsAt = Date.now() + 3000;
    }
  }

  confirmStake(id: string): void { this.player(id).stakeConfirmed = true; }

  move(id: string, next: Vec3, yaw: number, sequence: number): void {
    const p = this.player(id);
    if (!p.alive || this.state.phase !== 'playing' || sequence <= p.sequence) return;
    const elapsed = Math.max(.05, (Date.now() - this.lastTick) / 1000);
    if (distance(p.position, next) > 7.5 * elapsed + 1.25) return;
    p.position = { x: next.x, y: next.y, z: next.z }; p.yaw = yaw; p.sequence = sequence;
  }

  collect(id: string, orbId: string): void {
    if (this.state.phase !== 'playing') return;
    const p = this.player(id); const orb = this.state.orbs.find(o => o.id === orbId);
    if (!p.alive || !orb?.available || distance(p.position, orb.position) > 1.6) return;
    orb.available = false; p.carried++;
  }

  deposit(id: string): void {
    const p = this.player(id);
    if (!p.alive || p.carried < 1 || distance(p.position, this.state.bank) > 2.2) return;
    p.banked += p.carried; p.carried = 0; p.lastDepositAt = Date.now();
  }

  tick(now = Date.now(), seconds = 180): void {
    if (this.state.phase === 'countdown' && now >= this.state.endsAt) { this.state.phase = 'playing'; this.state.endsAt = now + seconds * 1000; }
    if (this.state.phase !== 'playing') { this.lastTick = now; return; }
    for (const p of this.state.players.filter(p => p.alive)) {
      if (this.state.traps.some(t => distance(p.position, t.position) <= t.radius)) { p.alive = false; p.carried = 0; }
    }
    const alive = this.state.players.filter(p => p.alive).length;
    const exhausted = this.state.orbs.every(o => !o.available) && this.state.players.every(p => p.carried === 0);
    if (now >= this.state.endsAt || alive === 0 || exhausted) this.finish(now >= this.state.endsAt ? 'timer' : alive === 0 ? 'all-eliminated' : 'all-orbs-banked');
    this.lastTick = now;
  }

  remove(id: string): void { const p = this.state.players.find(p => p.id === id); if (p && this.state.phase === 'playing') { p.alive = false; p.carried = 0; } else this.state.players = this.state.players.filter(p => p.id !== id); }

  private finish(reason: string): void {
    this.state.phase = 'finished'; this.state.finishReason = reason;
    const ranked = [...this.state.players].sort((a, b) => b.banked - a.banked || a.lastDepositAt - b.lastDepositAt);
    if (ranked.length && (ranked.length === 1 || ranked[0].banked !== ranked[1].banked || ranked[0].lastDepositAt !== ranked[1].lastDepositAt)) this.state.winnerId = ranked[0].id;
  }

  private player(id: string): PlayerState { const p = this.state.players.find(p => p.id === id); if (!p) throw new Error('Player is not in this match.'); return p; }
}
