using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MazeZero
{
    public sealed class MultiplayerClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "wss://multiplayer.maze-zero.example";
        [SerializeField] private float sendRate = 10f;
        private readonly Dictionary<string, RemoteAvatar> remotes = new();
        private PlayerController localPlayer;
        private string selfId;
        private int sequence;
        private float nextSend;
        public string MatchId { get; private set; }
        public string Phase { get; private set; }
        public int Carried { get; private set; }
        public int Banked { get; private set; }
        public event Action<NetworkState, string> StateReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void MazeZeroSocketConnect(string url);
        [DllImport("__Internal")] private static extern void MazeZeroSocketSend(string json);
        [DllImport("__Internal")] private static extern void MazeZeroSocketClose();
#endif

        private void Awake()
        {
            gameObject.name = "Multiplayer Client";
            localPlayer = FindFirstObjectByType<PlayerController>();
            if (!string.IsNullOrWhiteSpace(NimiqRunContext.ServerUrl)) serverUrl = NimiqRunContext.ServerUrl;
#if UNITY_WEBGL && !UNITY_EDITOR
            MazeZeroSocketConnect(serverUrl);
#else
            Debug.Log("Multiplayer WebSocket transport activates in a WebGL build.");
#endif
        }

        private void Update()
        {
            if (localPlayer == null) localPlayer = FindFirstObjectByType<PlayerController>();
            if (localPlayer != null && Time.unscaledTime >= nextSend && Phase == "playing")
            {
                nextSend = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
                var p = localPlayer.transform.position;
                Send($"{{\"type\":\"move\",\"position\":{{\"x\":{p.x:R},\"y\":{p.y:R},\"z\":{p.z:R}}},\"yaw\":{localPlayer.transform.eulerAngles.y:R},\"sequence\":{++sequence}}}");
            }
            foreach (var remote in remotes.Values) remote.Step(Time.deltaTime);
        }

        public void OnSocketOpen(string unused)
        {
            if (NimiqRunContext.Queue == "create-private") CreatePrivate(NimiqRunContext.StakeLuna);
            else if (NimiqRunContext.Queue == "join-private") JoinPrivate(NimiqRunContext.RoomCode);
            else JoinPublic();
        }
        public void OnSocketError(string message) => Debug.LogError($"Multiplayer: {message}");
        public void OnSocketClose(string unused) => Phase = "disconnected";

        public void JoinPublic() => Send($"{{\"type\":\"join_public\",\"authToken\":{Quote(NimiqRunContext.AuthToken)}}}");
        public void CreatePrivate(int stakeLuna) => Send($"{{\"type\":\"create_private\",\"authToken\":{Quote(NimiqRunContext.AuthToken)},\"stakeLuna\":{stakeLuna}}}");
        public void JoinPrivate(string code) => Send($"{{\"type\":\"join_private\",\"authToken\":{Quote(NimiqRunContext.AuthToken)},\"code\":{Quote(code)}}}");
        public void Ready() => Send("{\"type\":\"ready\"}");
        public void RequestCollect(string orbId) => Send($"{{\"type\":\"collect\",\"orbId\":{Quote(orbId)}}}");
        public void RequestDeposit() => Send("{\"type\":\"deposit\"}");
        public void ConfirmStake(string transactionHash) => Send($"{{\"type\":\"confirm_stake\",\"transactionHash\":{Quote(transactionHash)}}}");

        public void OnSocketMessage(string json)
        {
            var envelope = JsonUtility.FromJson<ServerEnvelope>(json);
            if (envelope == null) return;
            if (envelope.type == "error") { Debug.LogError($"Multiplayer: {envelope.message}"); return; }
            if (envelope.type != "state" || envelope.state == null) return;
            selfId = envelope.selfId; MatchId = envelope.state.id; Phase = envelope.state.phase;
            ApplyPlayers(envelope.state.players ?? Array.Empty<NetworkPlayer>());
            StateReceived?.Invoke(envelope.state, selfId);
        }

        private void ApplyPlayers(NetworkPlayer[] players)
        {
            var present = new HashSet<string>();
            foreach (var state in players)
            {
                present.Add(state.id);
                if (state.id == selfId)
                {
                    Carried = state.carried; Banked = state.banked;
                    if (!state.alive && localPlayer != null) localPlayer.CanMove = false;
                    continue;
                }
                if (!remotes.TryGetValue(state.id, out var remote)) remotes[state.id] = remote = RemoteAvatar.Create(state.id);
                remote.Target = new Vector3(state.position.x, state.position.y, state.position.z); remote.Yaw = state.yaw; remote.SetAlive(state.alive);
            }
            foreach (var id in new List<string>(remotes.Keys)) if (!present.Contains(id)) { remotes[id].Destroy(); remotes.Remove(id); }
        }

        public Transform FirstLivingRemote()
        {
            foreach (var remote in remotes.Values) if (remote.IsAlive) return remote.Transform;
            return null;
        }

        private static string Quote(string value) => $"\"{(value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        private static void Send(string json) {
#if UNITY_WEBGL && !UNITY_EDITOR
            MazeZeroSocketSend(json);
#else
            Debug.Log($"Multiplayer send: {json}");
#endif
        }
        private void OnDestroy() {
#if UNITY_WEBGL && !UNITY_EDITOR
            MazeZeroSocketClose();
#endif
        }

        [Serializable] private sealed class ServerEnvelope { public string type; public string selfId; public string message; public NetworkState state; }
        [Serializable] public sealed class NetworkState { public string id; public string phase; public int seed; public int mazeWidth; public int mazeHeight; public float cellSize; public long endsAt; public NetVector bank; public NetworkPlayer[] players; public NetworkOrb[] orbs; public NetworkTrap[] traps; public string winnerId; }
        [Serializable] public sealed class NetworkPlayer { public string id; public NetVector position; public float yaw; public bool alive; public int carried; public int banked; }
        [Serializable] public sealed class NetworkOrb { public string id; public NetVector position; public bool available; }
        [Serializable] public sealed class NetworkTrap { public string id; public NetVector position; public float radius; }
        [Serializable] public sealed class NetVector { public float x; public float y; public float z; public Vector3 Vector => new(x, y, z); }

        private sealed class RemoteAvatar
        {
            private readonly GameObject body; public Vector3 Target; public float Yaw; public bool IsAlive { get; private set; } = true; public Transform Transform => body.transform;
            private RemoteAvatar(GameObject body) { this.body = body; }
            public static RemoteAvatar Create(string id) { var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = $"Remote Player {id}"; UnityEngine.Object.Destroy(body.GetComponent<Collider>()); return new RemoteAvatar(body); }
            public void Step(float dt) { body.transform.position = Vector3.Lerp(body.transform.position, Target, 14f * dt); body.transform.rotation = Quaternion.Slerp(body.transform.rotation, Quaternion.Euler(0, Yaw, 0), 14f * dt); }
            public void SetAlive(bool alive) { IsAlive = alive; body.SetActive(alive); }
            public void Destroy() => UnityEngine.Object.Destroy(body);
        }
    }
}
