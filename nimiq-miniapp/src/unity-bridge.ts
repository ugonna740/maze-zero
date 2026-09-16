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
  const suffix = import.meta.env.VITE_UNITY_COMPRESSION_SUFFIX ?? '.gz';
  await injectScript(loaderUrl);
  if (!window.createUnityInstance) throw new Error('Unity loader did not expose createUnityInstance.');
  const assetUrls: string[] = [];
  try {
    const dataUrl = await unpackBuildFile(`${buildUrl}/${buildName}.data${suffix}`, 'application/octet-stream');
    assetUrls.push(dataUrl);
    onProgress(0.25);
    const frameworkUrl = await unpackBuildFile(`${buildUrl}/${buildName}.framework.js${suffix}`, 'application/javascript');
    assetUrls.push(frameworkUrl);
    onProgress(0.35);
    const codeUrl = await unpackBuildFile(`${buildUrl}/${buildName}.wasm${suffix}`, 'application/wasm');
    assetUrls.push(codeUrl);
    onProgress(0.5);
    unity = await window.createUnityInstance(canvas, {
      dataUrl,
      frameworkUrl,
      codeUrl,
      streamingAssetsUrl: '/unity/StreamingAssets',
      companyName: 'Maze Zero',
      productName: 'Maze Zero',
      productVersion: '0.1.0',
    }, value => onProgress(0.5 + value * 0.5));
  } finally {
    assetUrls.forEach(url => URL.revokeObjectURL(url));
  }
  if (pending) unity.SendMessage('Lobby UI', 'OnNimiqModeAuthorized', JSON.stringify(pending));
}

async function unpackBuildFile(url: string, contentType: string): Promise<string> {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Unity asset unavailable: ${url} (${response.status})`);
  const downloaded = await response.blob();
  const magic = new Uint8Array(await downloaded.slice(0, 2).arrayBuffer());
  let data = downloaded;
  // Pages can serve a pre-gzipped file as binary. Decode it before handing
  // the asset to Unity; a Blob URL then has the exact content type it expects.
  if (magic[0] === 0x1f && magic[1] === 0x8b) {
    if (typeof DecompressionStream === 'undefined')
      throw new Error('This phone needs iOS 16.4 or later to open the game.');
    data = await new Response(downloaded.stream().pipeThrough(new DecompressionStream('gzip'))).blob();
  }
  return URL.createObjectURL(new Blob([data], { type: contentType }));
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
