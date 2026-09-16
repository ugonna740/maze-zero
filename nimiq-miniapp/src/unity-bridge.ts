export type GameMode = 'practice' | 'multiplayer' | 'nim-arena';

export interface LaunchPayload {
  mode: GameMode;
  address?: string;
  deviceId?: string;
  signature?: string;
  challenge?: string;
  runId?: string;
  authToken?: string;
  serverUrl?: string;
  queue?: 'public' | 'create-private' | 'join-private';
  roomCode?: string;
  stakeLuna?: number;
}

type UnityInstance = { SendMessage(object: string, method: string, value?: string): void };

declare global {
  interface Window {
    createUnityInstance?: (canvas: HTMLCanvasElement, config: object, progress: (value: number) => void) => Promise<UnityInstance>;
    mazeZero?: { launchMode(payload: LaunchPayload): void; unityReady(): boolean; selectMode(mode: GameMode): void };
  }
}

let unity: UnityInstance | null = null;
let pending: LaunchPayload | null = null;

export function unityReady(): boolean { return unity !== null; }

export function launchMode(payload: LaunchPayload): void {
  pending = payload;
  if (unity) unity.SendMessage('Lobby UI', 'OnNimiqModeAuthorized', JSON.stringify(payload));
}

export async function mountUnity(canvas: HTMLCanvasElement, onProgress: (value: number) => void): Promise<void> {
  const loaderUrl = import.meta.env.VITE_UNITY_LOADER_URL || '/unity/Build/unity.loader.js';
  const buildUrl = import.meta.env.VITE_UNITY_BUILD_URL || '/unity/Build';
  const buildName = import.meta.env.VITE_UNITY_BUILD_NAME || 'unity';
  const suffix = import.meta.env.VITE_UNITY_COMPRESSION_SUFFIX ?? '.unityweb';
  await injectScript(loaderUrl);
  if (!window.createUnityInstance) throw new Error('Unity loader did not expose createUnityInstance.');
  unity = await window.createUnityInstance(canvas, {
    dataUrl: `${buildUrl}/${buildName}.data${suffix}`,
    frameworkUrl: `${buildUrl}/${buildName}.framework.js${suffix}`,
    codeUrl: `${buildUrl}/${buildName}.wasm${suffix}`,
    streamingAssetsUrl: '/unity/StreamingAssets',
    companyName: 'Maze Zero',
    productName: 'Maze Zero',
    productVersion: '0.1.0',
  }, onProgress);
  if (pending) unity.SendMessage('Lobby UI', 'OnNimiqModeAuthorized', JSON.stringify(pending));
}

function injectScript(src: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = src;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error(`Unity WebGL build not found at ${src}`));
    document.body.appendChild(script);
  });
}
