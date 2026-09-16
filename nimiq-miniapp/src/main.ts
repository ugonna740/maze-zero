import './styles.css';
import './match-menu.css';
import './landscape.css';
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
    <div class="stage"><canvas id="unity-canvas"></canvas><div id="cover"><div class="mark">MZ</div><p id="status">Preparing Maze Zero…</p><div class="bar"><span id="progress"></span></div><p id="load-error-details" hidden></p><button id="retry-load" type="button" hidden>Retry loading</button></div></div>
    <footer><span>Fair runs</span><span>One life</span><span>Powered by Nimiq</span></footer>
  </section>
  <section id="rotation-prompt" class="rotation-prompt" role="status" aria-live="polite" hidden>
    <div class="rotation-card">
      <span class="rotation-kicker">MAZE ZERO · BEFORE YOU PLAY</span>
      <div class="rotation-visual" aria-hidden="true"><div class="rotation-phone"><div class="rotation-screen">MZ</div></div><span class="rotation-arrow">↻</span></div>
      <h2>Rotate your phone</h2>
      <p>Maze Zero is made for landscape. Turn your phone sideways to see the full maze, controls and minimap.</p>
      <small>If the screen stays upright, turn off portrait orientation lock.</small>
    </div>
  </section>`;

const status = document.querySelector<HTMLElement>('#status')!;
const cover = document.querySelector<HTMLElement>('#cover')!;
const progress = document.querySelector<HTMLElement>('#progress')!;
const wallet = document.querySelector<HTMLButtonElement>('#wallet')!;
const matchMenu = document.querySelector<HTMLElement>('#match-menu')!;
const shell = document.querySelector<HTMLElement>('.shell')!;
const retryLoad = document.querySelector<HTMLButtonElement>('#retry-load')!;
const errorDetails = document.querySelector<HTMLElement>('#load-error-details')!;
const rotationPrompt = document.querySelector<HTMLElement>('#rotation-prompt')!;
const portraitPhone = matchMedia('(orientation: portrait) and (max-width: 900px)');
let authorized: LaunchPayload | null = null;
let loadPromise: Promise<void> | null = null;

function showLoadError(error: unknown) {
  const detail = error instanceof Error ? error.message : String(error);
  status.textContent = 'Game could not load on this device';
  errorDetails.textContent = detail || 'Unknown Unity loading error';
  errorDetails.hidden = false;
  retryLoad.hidden = false;
  cover.classList.remove('hidden');
  progress.style.width = '0%';
}

function ensureUnityLoaded(): Promise<void> {
  if (!loadPromise) {
    retryLoad.hidden = true;
    errorDetails.hidden = true;
    status.textContent = 'Loading Maze Zero…';
    loadPromise = mountUnity(document.querySelector<HTMLCanvasElement>('#unity-canvas')!, value => {
      progress.style.width = `${Math.round(value * 100)}%`;
      status.textContent = `Loading Maze Zero · ${Math.round(value * 100)}%`;
    }).then(() => {
      status.textContent = 'Choose your run';
      progress.style.width = '100%';
    }).catch(error => {
      showLoadError(error);
      throw error;
    });
  }
  return loadPromise;
}

function syncOrientation() {
  const needsRotation = portraitPhone.matches;
  rotationPrompt.hidden = !needsRotation;
  document.body.classList.toggle('needs-rotation', needsRotation);
  if (!needsRotation) void ensureUnityLoaded().catch(() => {});
}

async function selectMode(mode: GameMode) {
  try {
    if (mode === 'nim-arena') throw new Error('NIM Arena is not live yet. Please choose Practice or Public Multiplayer.');
    status.textContent = mode === 'practice' ? 'Generating an unranked maze…' : 'Confirm your Nimiq identity…';
    if (mode === 'practice') {
      await ensureUnityLoaded();
      launchMode({ mode });
      shell.classList.add('is-playing');
    }
    else {
      authorized = await authorizeMode(mode);
      await ensureUnityLoaded();
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
    if (retryLoad.hidden) status.textContent = error instanceof Error ? error.message : 'Open Maze Zero inside Nimiq Pay to connect.';
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
  shell.classList.add('is-playing');
  matchMenu.classList.add('hidden'); cover.classList.add('hidden');
}
window.addEventListener('maze-zero-request-mode', event => {
  const mode = (event as CustomEvent<{ mode: GameMode }>).detail?.mode;
  if (mode === 'multiplayer' || mode === 'nim-arena') void selectMode(mode);
});
window.mazeZero = { launchMode, selectMode, unityReady };
retryLoad.addEventListener('click', () => location.reload());
portraitPhone.addEventListener('change', syncOrientation);
window.addEventListener('resize', syncOrientation);
syncOrientation();
