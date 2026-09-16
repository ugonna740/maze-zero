import type { OrbState, Vec3 } from './protocol.js';

class MazeRandom {
  private state: number;
  constructor(seed: number) { this.state = seed >>> 0 || 0x6d2b79f5; }
  private uint(): number { let x = this.state; x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return this.state = x >>> 0; }
  next(max: number): number { return max <= 1 ? 0 : this.uint() % max; }
  double(): number { return this.uint() / 4294967296; }
}

export interface MazeData { orbs: OrbState[]; bank: Vec3 }

export function generateMazeObjects(width: number, height: number, cellSize: number, seed: number): MazeData {
  const random = new MazeRandom(seed);
  const visited = Array.from({ length: width }, () => Array<boolean>(height).fill(false));
  const distance = Array.from({ length: width }, () => Array<number>(height).fill(0));
  const stack: Array<[number, number]> = [[0, 0]]; visited[0][0] = true;
  const directions: Array<[number, number]> = [[0, 1], [1, 0], [0, -1], [-1, 0]];
  let exit: [number, number] = [0, 0];
  while (stack.length) {
    const [x, y] = stack[stack.length - 1];
    const options = directions.map((_, i) => i).filter(i => { const nx = x + directions[i][0], ny = y + directions[i][1]; return nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx][ny]; });
    if (!options.length) { stack.pop(); continue; }
    const direction = directions[options[random.next(options.length)]]; const nx = x + direction[0], ny = y + direction[1];
    visited[nx][ny] = true; distance[nx][ny] = distance[x][y] + 1;
    if (distance[nx][ny] > distance[exit[0]][exit[1]]) exit = [nx, ny];
    stack.push([nx, ny]);
  }
  const bankCell = { x: Math.floor(width / 2), y: Math.floor(height / 2) };
  const orbRandom = new MazeRandom((seed ^ 0x5f3759df) | 0); const orbs: OrbState[] = [];
  for (let x = 0; x < width; x++) for (let y = 0; y < height; y++) {
    if ((x !== 0 || y !== 0) && (x !== exit[0] || y !== exit[1]) && (x !== bankCell.x || y !== bankCell.y) && orbRandom.double() < .48) orbs.push({ id: `orb-${x}-${y}`, position: { x: x * cellSize, y: 1.05, z: y * cellSize }, available: true });
  }
  return { orbs, bank: { x: bankCell.x * cellSize, y: 0, z: bankCell.y * cellSize } };
}
