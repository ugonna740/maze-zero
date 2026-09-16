using System.Collections.Generic;
using UnityEngine;

namespace MazeZero
{
    public sealed class TrapManager : MonoBehaviour
    {
        [Header("Drop Your Trap Prefabs Here")]
        [SerializeField] private List<GameObject> trapPrefabs = new();

        [Header("Procedural Placement")]
        [SerializeField, Min(0)] private int minimumTraps = 5;
        [SerializeField, Min(0)] private int maximumTraps = 12;
        [SerializeField, Min(0f)] private float safeRadiusFromStart = 2.5f;
        [SerializeField] private bool randomizeYRotation = true;
        [SerializeField] private bool removeOrbFromTrapCell = true;
        [SerializeField] private float verticalOffset;

        public int Scatter(Transform runOwner, MazeBuilder builder, MazeLayout layout, int seed)
        {
            var availablePrefabs = trapPrefabs.FindAll(prefab => prefab != null);
            if (availablePrefabs.Count == 0) return 0;

            var random = new System.Random(seed ^ 0x21f0aaad);
            var candidates = new List<Vector2Int>();
            for (var x = 0; x < layout.Width; x++)
            for (var y = 0; y < layout.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (cell == layout.Start || cell == layout.Exit) continue;
                if (Vector2Int.Distance(cell, layout.Start) <= safeRadiusFromStart) continue;
                candidates.Add(cell);
            }

            var low = Mathf.Clamp(minimumTraps, 0, candidates.Count);
            var high = Mathf.Clamp(Mathf.Max(low, maximumTraps), low, candidates.Count);
            var count = high > low ? random.Next(low, high + 1) : low;
            var container = new GameObject("Generated Traps");
            container.transform.SetParent(runOwner);

            for (var i = 0; i < count; i++)
            {
                var cellIndex = random.Next(candidates.Count);
                var cell = candidates[cellIndex];
                candidates.RemoveAt(cellIndex);
                if (removeOrbFromTrapCell) builder.OrbCells.Remove(cell);

                var prefab = availablePrefabs[random.Next(availablePrefabs.Count)];
                var trap = Instantiate(prefab, container.transform);
                trap.name = prefab.name + " [Generated]";
                var yOffset = IsPunchTrap(prefab) ? .2f : verticalOffset;
                trap.transform.position = builder.CellWorld(cell) + Vector3.up * yOffset;
                if (randomizeYRotation) trap.transform.rotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);
                var damage = trap.GetComponentInChildren<TrapDamage>(true);
                if (damage == null)
                {
                    damage = trap.AddComponent<TrapDamage>();
                    damage.ConfigureFallback(prefab.name, random.Next());
                    Debug.LogWarning($"{prefab.name} has no TrapDamage component. A varied fallback profile was added to this generated instance.", trap);
                }
            }
            return count;
        }

        public GameObject SpawnNetworkTrap(Transform owner, Vector3 position, int variation)
        {
            var available = trapPrefabs.FindAll(prefab => prefab != null);
            if (available.Count == 0) return null;
            var prefab = available[Mathf.Abs(variation) % available.Count];
            var trap = Instantiate(prefab, owner); trap.name = prefab.name + " [Network]";
            trap.transform.position = position + Vector3.up * (IsPunchTrap(prefab) ? .2f : verticalOffset);
            if (randomizeYRotation) trap.transform.rotation = Quaternion.Euler(0, Mathf.Abs(variation * 97) % 4 * 90f, 0);
            foreach (var damage in trap.GetComponentsInChildren<TrapDamage>(true)) Destroy(damage);
            return trap;
        }

        private static bool IsPunchTrap(GameObject prefab)
        {
            var normalizedName = prefab.name.Replace(" ", "").Replace("_", "").Replace("-", "");
            return normalizedName.IndexOf("punchtrap", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
