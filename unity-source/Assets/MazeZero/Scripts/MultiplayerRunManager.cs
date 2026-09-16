using System.Collections.Generic;
using UnityEngine;

namespace MazeZero
{
    public sealed class MultiplayerRunManager : MonoBehaviour
    {
        private readonly Dictionary<string, GameObject> orbObjects = new();
        private MultiplayerClient client;
        private PlayerController player;
        private bool built;
        private bool spectating;
        private MultiplayerHud multiplayerHud;

        private void Start()
        {
            player = GetComponentInChildren<PlayerController>(true) ?? FindFirstObjectByType<PlayerController>();
            client = GetComponent<MultiplayerClient>() ?? gameObject.AddComponent<MultiplayerClient>();
            client.StateReceived += ApplyState;
        }

        private void OnDestroy() { if (client != null) client.StateReceived -= ApplyState; }

        private void ApplyState(MultiplayerClient.NetworkState state, string selfId)
        {
            if (!built) BuildWorld(state, selfId);
            if (multiplayerHud != null) multiplayerHud.Refresh(state, selfId);
            foreach (var orb in state.orbs ?? System.Array.Empty<MultiplayerClient.NetworkOrb>())
                if (orbObjects.TryGetValue(orb.id, out var body)) body.SetActive(orb.available);
            var self = System.Array.Find(state.players, p => p.id == selfId);
            if (self != null && !self.alive && player != null)
            {
                player.CanMove = false;
                if (!spectating)
                {
                    spectating = true;
                    var target = client.FirstLivingRemote();
                    var camera = FindFirstObjectByType<ThirdPersonCamera>();
                    if (camera != null && target != null) camera.Follow(target);
                }
            }
        }

        private void BuildWorld(MultiplayerClient.NetworkState state, string selfId)
        {
            built = true;
            ClearOldGeneratedWorld();
            var root = new GameObject("Generated Multiplayer Match"); root.transform.SetParent(transform);
            multiplayerHud = GetComponentInChildren<MultiplayerHud>(true) ?? MultiplayerHud.Create(transform);
            var maze = new GameObject("Shared Maze"); maze.transform.SetParent(root.transform);
            maze.AddComponent<MazeBuilder>().Build(state.mazeWidth, state.mazeHeight, state.seed);
            foreach (var orb in state.orbs ?? System.Array.Empty<MultiplayerClient.NetworkOrb>()) SpawnOrb(root.transform, orb);
            SpawnBank(root.transform, state.bank.Vector);
            SpawnTraps(root.transform, state);

            var self = System.Array.Find(state.players, p => p.id == selfId);
            if (player != null && self != null) player.SetSpawnPosition(self.position.Vector + Vector3.down * 2.5f);
            var camera = GetComponentInChildren<ThirdPersonCamera>(true) ?? FindFirstObjectByType<ThirdPersonCamera>();
            if (camera != null && player != null) camera.Follow(player.transform);
            client.Ready();
        }

        private void SpawnOrb(Transform owner, MultiplayerClient.NetworkOrb state)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name = state.id; orb.transform.SetParent(owner); orb.transform.position = state.position.Vector; orb.transform.localScale = Vector3.one * .48f;
            Destroy(orb.GetComponent<Collider>()); var trigger = orb.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = 1.05f;
            orb.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MaterialWithColor("Network Orb", new Color(.1f, .9f, 1f), true);
            var pickup = orb.AddComponent<MultiplayerOrb>(); pickup.Configure(state.id, client); orbObjects[state.id] = orb;
        }

        private void SpawnBank(Transform owner, Vector3 position)
        {
            var bank = GameObject.CreatePrimitive(PrimitiveType.Cylinder); bank.name = "Orb Bank"; bank.transform.SetParent(owner); bank.transform.position = position + Vector3.up * .12f; bank.transform.localScale = new Vector3(1.7f, .12f, 1.7f);
            Destroy(bank.GetComponent<Collider>()); var trigger = bank.AddComponent<CapsuleCollider>(); trigger.isTrigger = true; trigger.radius = 1.25f; trigger.height = 2.5f; trigger.center = new Vector3(0, 1.1f, 0);
            bank.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MaterialWithColor("Orb Bank", new Color(1f, .65f, .08f), true);
            bank.AddComponent<MultiplayerBank>().Configure(client);
        }

        private void SpawnTraps(Transform owner, MultiplayerClient.NetworkState state)
        {
            var trapManager = FindFirstObjectByType<TrapManager>();
            var container = new GameObject("Network Traps"); container.transform.SetParent(owner);
            var traps = state.traps ?? System.Array.Empty<MultiplayerClient.NetworkTrap>();
            for (var i = 0; i < traps.Length; i++)
            {
                var spawned = trapManager != null ? trapManager.SpawnNetworkTrap(container.transform, traps[i].position.Vector, state.seed + i) : null;
                if (spawned != null) continue;
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.name = traps[i].id; marker.transform.SetParent(container.transform); marker.transform.position = traps[i].position.Vector + Vector3.up * .15f; marker.transform.localScale = new Vector3(.75f, .15f, .75f);
                marker.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MaterialWithColor("Network Trap", new Color(.85f, .12f, .08f), true);
            }
        }

        private void ClearOldGeneratedWorld()
        {
            for (var i = transform.childCount - 1; i >= 0; i--) if (transform.GetChild(i).name.StartsWith("Generated")) Destroy(transform.GetChild(i).gameObject);
            foreach (var exit in GetComponentsInChildren<ExitController>(true)) exit.gameObject.SetActive(false);
        }
    }

    public sealed class MultiplayerOrb : MonoBehaviour
    {
        private string orbId; private MultiplayerClient client; private bool requested;
        public void Configure(string id, MultiplayerClient owner) { orbId = id; client = owner; }
        private void Update() => transform.Rotate(0, 100f * Time.deltaTime, 0, Space.World);
        private void OnTriggerEnter(Collider other) { if (requested || !other.TryGetComponent<PlayerController>(out _)) return; requested = true; client.RequestCollect(orbId); }
    }

    public sealed class MultiplayerBank : MonoBehaviour
    {
        private MultiplayerClient client; private float nextRequest;
        public void Configure(MultiplayerClient owner) => client = owner;
        private void OnTriggerStay(Collider other) { if (Time.time < nextRequest || !other.TryGetComponent<PlayerController>(out _)) return; nextRequest = Time.time + .5f; client.RequestDeposit(); }
    }
}
