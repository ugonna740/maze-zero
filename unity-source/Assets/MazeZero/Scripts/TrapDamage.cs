using UnityEngine;

namespace MazeZero
{
    [DisallowMultipleComponent]
    public sealed class TrapDamage : MonoBehaviour
    {
        [Header("Damage - Configure Per Prefab")]
        [SerializeField] private bool instantGameOver;
        [SerializeField, Min(0)] private int orbPenalty = 3;
        [SerializeField, Min(0f)] private float timePenalty;
        [SerializeField, InspectorName("Game Over On Hit")] private bool respawnPlayer = true;
        [SerializeField, Min(.1f)] private float repeatCooldown = 1.5f;

        private float nextAllowedHit;

        public bool InstantGameOver => instantGameOver;
        public int OrbPenalty => orbPenalty;
        public float TimePenalty => timePenalty;
        public bool RespawnPlayer => respawnPlayer;
        public bool GameOverOnHit => respawnPlayer;

        public void ConfigureFallback(string prefabName, int variation)
        {
            var value = prefabName.ToLowerInvariant();
            instantGameOver = value.Contains("press") || value.Contains("gyotine") || value.Contains("spikewall");
            if (instantGameOver)
            {
                orbPenalty = 0;
                timePenalty = 0f;
                respawnPlayer = false;
            }
            else if (value.Contains("blade") || value.Contains("hammer") || value.Contains("punch"))
            {
                orbPenalty = 4 + Mathf.Abs(variation % 5);
                timePenalty = value.Contains("hammer") ? 4f : 0f;
                respawnPlayer = true;
            }
            else if (value.Contains("oil") || value.Contains("ice"))
            {
                orbPenalty = 2 + Mathf.Abs(variation % 3);
                timePenalty = 5f;
                respawnPlayer = false;
            }
            else
            {
                orbPenalty = 3;
                timePenalty = 3f;
                respawnPlayer = true;
            }
        }

        public void TryApply(PlayerController player, Vector3? hitPosition = null)
        {
            if (player == null || Time.time < nextAllowedHit || PracticeRunManager.Instance == null) return;
            nextAllowedHit = Time.time + repeatCooldown;
            PracticeRunManager.Instance.ApplyTrapDamage(this, hitPosition);
        }
    }
}
