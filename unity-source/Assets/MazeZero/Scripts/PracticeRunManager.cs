using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeZero
{
    public sealed class PracticeRunManager : MonoBehaviour
    {
        public static PracticeRunManager Instance { get; private set; }
        [Header("Editable Run Settings")]
        [SerializeField] private int mazeWidth = 12;
        [SerializeField] private int mazeHeight = 12;
        [SerializeField] private int previewSeed = 428194;
        [SerializeField] private float runDuration = 180f;
        [SerializeField] private float characterVisualGroundOffset = -0.8f;
        private float remaining;
        private int collected;
        private int totalOrbs;
        private int seed;
        private bool finished;
        private PlayerController player;
        private PracticeHud hud;
        private float trapImmunityUntil;

        private void Awake() { Instance = this; }

        private void Start()
        {
            if (NimiqRunContext.IsMultiplayer)
            {
                gameObject.AddComponent<MultiplayerRunManager>();
                enabled = false;
                return;
            }
            // Runtime gameplay is regenerated without modifying the serialized editor scene.
            // Player, camera and HUD objects are preserved by StartRun.
            StartRun();
        }

        private void Update()
        {
            if (finished)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) StartRun();
                return;
            }
            remaining -= Time.deltaTime;
            if (hud == null) hud = GetComponentInChildren<PracticeHud>(true);
            if (hud == null) return;
            hud.Refresh(remaining, collected, totalOrbs, seed, runDuration);
            if (remaining <= 0f) Finish(false);
        }

        public void CollectOrb() { if (!finished) collected++; }
        public void Escape() { if (!finished) Finish(true); }
        public void HitTrap()
        {
            if (finished || Time.time < trapImmunityUntil) return;
            trapImmunityUntil = Time.time + 1.5f;
            PlayTrapHitFeedback(null, true);
            Finish(false, "TRAP FATALITY");
        }

        public void RestartRun()
        {
            if (finished) StartRun();
        }

        public void ApplyTrapDamage(TrapDamage damage, Vector3? hitPosition = null)
        {
            if (finished || damage == null) return;
            PlayTrapHitFeedback(hitPosition, damage.InstantGameOver || damage.GameOverOnHit);
            if (damage.InstantGameOver)
            {
                Finish(false, "TRAP FATALITY");
                return;
            }

            var lostOrbs = Mathf.Min(collected, damage.OrbPenalty);
            collected -= lostOrbs;
            remaining = Mathf.Max(0f, remaining - damage.TimePenalty);

            var warning = lostOrbs > 0 && damage.TimePenalty > 0f
                ? $"TRAP HIT  -{lostOrbs} ORBS  -{damage.TimePenalty:0}s"
                : lostOrbs > 0
                    ? $"TRAP HIT  -{lostOrbs} ORBS"
                    : damage.TimePenalty > 0f
                        ? $"TRAP HIT  -{damage.TimePenalty:0} SECONDS"
                        : "TRAP HIT";
            if (damage.GameOverOnHit)
            {
                Finish(false, "TRAP FATALITY");
                return;
            }
            if (hud != null) hud.ShowTrapWarning(warning);
            if (remaining <= 0f) Finish(false, "TIME DESTROYED");
        }

        private void PlayTrapHitFeedback(Vector3? hitPosition, bool fatal)
        {
            var followCamera = GetComponentInChildren<ThirdPersonCamera>();
            if (followCamera != null) followCamera.ShakeOnHit(fatal);
            if (player == null) return;
            var body = player.GetComponent<CharacterController>();
            var position = hitPosition ?? (body != null ? body.bounds.center : player.transform.position);
            TrapHitEffect.Spawn(position, transform);
        }

        public void BuildPreviewInEditor()
        {
            ClearAllRunObjects();
            seed = previewSeed;
            BuildRun();
        }

        private void StartRun()
        {
            var existingPlayer = GetComponentInChildren<PlayerController>(true);
            var existingHud = GetComponentInChildren<PracticeHud>(true);
            var existingCamera = GetComponentInChildren<ThirdPersonCamera>(true);
            ClearGeneratedObjects(existingHud);
            seed = GenerateRunSeed();
            BuildRun(existingPlayer, existingHud, existingCamera);
        }

        private static int GenerateRunSeed()
        {
            // Guid avoids Unity's editor Random state repeating between Play sessions.
            return 100000 + (System.Guid.NewGuid().GetHashCode() & 0x7fffffff) % 900000;
        }

        private void BuildRun(PlayerController existingPlayer = null, PracticeHud existingHud = null, ThirdPersonCamera existingCamera = null)
        {
            remaining = runDuration;
            collected = 0;
            finished = false;

            var generatedRoot = new GameObject("Generated Gameplay");
            generatedRoot.transform.SetParent(transform);
            var world = new GameObject("Generated Practice Run");
            world.transform.SetParent(generatedRoot.transform);
            var builder = world.AddComponent<MazeBuilder>();
            var layout = builder.Build(mazeWidth, mazeHeight, seed);
            var trapManager = FindFirstObjectByType<TrapManager>();
            if (trapManager != null) trapManager.Scatter(generatedRoot.transform, builder, layout, seed);
            totalOrbs = builder.OrbCells.Count;
            foreach (var cell in builder.OrbCells) SpawnOrb(builder.CellWorld(cell), generatedRoot.transform);
            SpawnExit(builder.CellWorld(layout.Exit), generatedRoot.transform);
            player = existingPlayer != null ? existingPlayer : SpawnPlayer(builder.CellWorld(layout.Start));
            if (existingPlayer != null) player.SetSpawnPosition(PlayerSpawnPosition(builder.CellWorld(layout.Start)));
            hud = existingHud != null ? existingHud : PracticeHud.Create();
            if (existingHud == null)
            {
                hud.transform.SetParent(transform);
                OnScreenControls.Create(hud.transform);
            }
            hud.BeginRun();
            var followCamera = existingCamera != null ? existingCamera : GetComponentInChildren<ThirdPersonCamera>(true);
            if (followCamera != null) followCamera.Follow(player.transform);
            MiniMap.Create(generatedRoot.transform, hud.transform, player.transform, layout.Width, layout.Height, builder.CellSize);
            hud.Refresh(remaining, collected, totalOrbs, seed, runDuration);
        }

        private static Vector3 PlayerSpawnPosition(Vector3 cellPosition) => cellPosition + Vector3.down * 2.5f;


        private void AdoptBakedScene()
        {
            remaining = runDuration;
            collected = 0;
            finished = false;
            seed = previewSeed;
            player = GetComponentInChildren<PlayerController>(true);
            hud = GetComponentInChildren<PracticeHud>(true);
            totalOrbs = GetComponentsInChildren<OrbCollectible>(true).Length;
            var followCamera = GetComponentInChildren<ThirdPersonCamera>(true);
            if (followCamera != null && player != null) followCamera.Follow(player.transform);
            if (hud != null) hud.Refresh(remaining, collected, totalOrbs, seed, runDuration);
        }

        private void ClearAllRunObjects()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void ClearGeneratedObjects(PracticeHud existingHud)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponentInChildren<PlayerController>(true) != null) continue;
                if (child.GetComponentInChildren<PracticeHud>(true) != null) continue;
                if (child.GetComponentInChildren<ThirdPersonCamera>(true) != null) continue;
                var generated = child.name.StartsWith("Generated")
                    || child.GetComponent<MazeBuilder>() != null
                    || child.GetComponent<OrbCollectible>() != null
                    || child.GetComponent<ExitController>() != null
                    || child.GetComponent<MiniMap>() != null;
                if (!generated) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            if (existingHud == null) return;
            var staleMap = existingHud.transform.Find("MiniMap");
            if (staleMap == null) return;
            if (Application.isPlaying) Destroy(staleMap.gameObject);
            else DestroyImmediate(staleMap.gameObject);
        }

        private PlayerController SpawnPlayer(Vector3 position)
        {
            var playerObject = new GameObject("Player", typeof(CharacterController));
            playerObject.transform.SetParent(transform);
            var character = playerObject.GetComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = .35f;
            character.center = new Vector3(0, 3.5f, 0);
            playerObject.transform.position = PlayerSpawnPosition(position);
            SpawnCharacterVisual(playerObject.transform, characterVisualGroundOffset);
            var cameraObject = new GameObject("Third Person Camera", typeof(Camera), typeof(AudioListener), typeof(ThirdPersonCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            cameraObject.GetComponent<Camera>().fieldOfView = 56f;
            cameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            cameraObject.GetComponent<Camera>().backgroundColor = new Color(.45f, .68f, .9f);
            cameraObject.GetComponent<ThirdPersonCamera>().Follow(playerObject.transform);
            return playerObject.AddComponent<PlayerController>();
        }

        private static void SpawnCharacterVisual(Transform parent, float groundOffset)
        {
            var characterPrefab = Resources.Load<GameObject>("MazeZeroCharacter");
            if (characterPrefab == null)
            {
                Debug.LogWarning("MazeZeroCharacter prefab has not been generated yet. Reimport the FBX or restart Unity.");
                return;
            }
            var visual = Instantiate(characterPrefab, parent);
            visual.name = "Character Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.y > .01f) visual.transform.localScale *= 1.7f / bounds.size.y;
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += Vector3.up * (-bounds.min.y + groundOffset);
        }

        private void SpawnOrb(Vector3 position, Transform owner)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb";
            orb.transform.SetParent(owner);
            orb.transform.position = position + Vector3.up * 1.05f;
            orb.transform.localScale = Vector3.one * .48f;
            DestroyRunObject(orb.GetComponent<Collider>());
            var trigger = orb.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.05f;
            orb.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MaterialWithColor("Orb Glow", new Color(.1f, .9f, 1f), true);
            orb.AddComponent<OrbCollectible>();
            var light = orb.AddComponent<Light>();
            light.color = new Color(.1f, .85f, 1f);
            light.range = 4.5f;
            light.intensity = 4f;
        }

        private void SpawnExit(Vector3 position, Transform owner)
        {
            var exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exit.name = "Exit Beacon";
            exit.transform.SetParent(owner);
            exit.transform.position = position + Vector3.up * .08f;
            exit.transform.localScale = new Vector3(1.2f, .08f, 1.2f);
            DestroyRunObject(exit.GetComponent<Collider>());
            var trigger = exit.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            trigger.height = 2.5f;
            trigger.center = new Vector3(0, 1.1f, 0);
            exit.GetComponent<Renderer>().sharedMaterial = MazeBuilder.MaterialWithColor("Exit Glow", new Color(.2f, 1f, .35f), true);
            exit.AddComponent<ExitController>();
            var light = exit.AddComponent<Light>();
            light.color = Color.green;
            light.range = 7f;
            light.intensity = 4f;
        }

        private void Finish(bool escaped, string failureTitle = "DETONATED")
        {
            finished = true;
            remaining = Mathf.Max(0, remaining);
            player.CanMove = false;
            hud.ShowResult(escaped, collected, runDuration - remaining, failureTitle);
        }

        private static void DestroyRunObject(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
