import './styles.css';
import './match-menu.css';
import { authorizeMode, shortAddress } from './nimiq';
import { launchMode, mountUnity, unityReady, type GameMode, type LaunchPayload } from './unity-bridge';

const app = document.querySelector<HTMLElement>('#app')!;
app.innerHTML = `
  <section class="shell">
    <header><div><span class="eyebrow">NIMIQ MINI APP</span><h1>MAZE <i>ZERO</i></h1></div><button id="wallet">Connect Nimiq</button></header>
    <nav aria-label="Game modes">
      <button data-mode="practice"><b>Practice Maze</b><small>Instant · no wallet</small></button>
      <button data-mode="multiplayer"><b>Multiplayer</b><small>Live public matches · no stake</small></button>
      <button data-mode="nim-arena" disabled aria-disabled="true"><b>NIM Arena</b><small>Coming after mainnet payout review</small></button>
    </nav>
    <section id="match-menu" class="match-menu hidden">
      <button id="public-match"><b>Find Public Match</b><small>Live matchmaking · no stake</small></button>
      <div><b>Private Stake Match</b><small>Temporarily unavailable while mainnet deposits, refunds and payouts are verified.</small></div>
    </section>
    <div class="stage"><canvas id="unity-canvas"></canvas><div id="cover"><div class="mark">MZ</div><p id="status">Loading Maze Zero…</p><div class="bar"><span id="progress"></span></div></div></div>
    <footer><span>Fair runs</span><span>One life</span><span>Powered by Nimiq</span></footer>
  </section>`;

const status = document.querySelector<HTMLElement>('#status')!;
const cover = document.querySelector<HTMLElement>('#cover')!;
const progress = document.querySelector<HTMLElement>('#progress')!;
const wallet = document.querySelector<HTMLButtonElement>('#wallet')!;
const matchMenu = document.querySelector<HTMLElement>('#match-menu')!;
let authorized: LaunchPayload | null = null;

async function selectMode(mode: GameMode) {
  try {
    if (mode === 'nim-arena') throw new Error('NIM Arena is not live yet. Please choose Practice or Public Multiplayer.');
    status.textContent = mode === 'practice' ? 'Generating an unranked maze…' : 'Confirm your Nimiq identity…';
    if (mode === 'practice') launchMode({ mode });
    else {
      authorized = await authorizeMode(mode);
      wallet.textContent = shortAddress(authorized.address);
      if (mode === 'multiplayer') {
        matchMenu.classList.remove('hidden');
        status.textContent = 'Public matchmaking is ready to launch';
        return;
      }
      launchMode(authorized);
    }
    cover.classList.add('hidden');
  } catch (error) {
    status.textContent = error instanceof Error ? error.message : 'Open Maze Zero inside Nimiq Pay to connect.';
    cover.classList.remove('hidden');
  }
}

document.querySelectorAll<HTMLButtonElement>('[data-mode]').forEach(button => {
  button.addEventListener('click', () => selectMode(button.dataset.mode as GameMode));
});
wallet.addEventListener('click', () => selectMode('multiplayer'));
document.querySelector<HTMLButtonElement>('#public-match')!.addEventListener('click', () => authorized && launchMultiplayer({ queue: 'public' }));

function launchMultiplayer(selection: Pick<LaunchPayload, 'queue' | 'roomCode' | 'stakeLuna'>) {
  launchMode({ ...authorized!, ...selection, mode: 'multiplayer' });
  matchMenu.classList.add('hidden'); cover.classList.add('hidden');
}
window.addEventListener('maze-zero-request-mode', event => {
  const mode = (event as CustomEvent<{ mode: GameMode }>).detail?.mode;
  if (mode === 'multiplayer' || mode === 'nim-arena') void selectMode(mode);
});
window.mazeZero = { launchMode, selectMode, unityReady };

mountUnity(document.querySelector<HTMLCanvasElement>('#unity-canvas')!, value => {
  progress.style.width = `${Math.round(value * 100)}%`;
  status.textContent = `Loading Maze Zero · ${Math.round(value * 100)}%`;
}).then(() => {
  status.textContent = 'Choose your run';
  progress.style.width = '100%';
}).catch(error => {
  status.textContent = error instanceof Error ? error.message : 'Unity build is not installed yet.';
});
