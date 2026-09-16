using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class MultiplayerHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text leaderboardText;

        public static MultiplayerHud Create(Transform owner)
        {
            var canvasObject = new GameObject("Multiplayer HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MultiplayerHud));
            canvasObject.transform.SetParent(owner); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 35;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var hud = canvasObject.GetComponent<MultiplayerHud>();
            hud.phaseText = Text("Match Status", new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(520, 55), 34, TextAlignmentOptions.Center, canvasObject.transform);
            hud.scoreText = Text("Bank Score", new Vector2(0, 1), new Vector2(30, -30), new Vector2(420, 90), 30, TextAlignmentOptions.TopLeft, canvasObject.transform);
            hud.leaderboardText = Text("Live Leaderboard", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(430, 250), 25, TextAlignmentOptions.TopRight, canvasObject.transform);
            return hud;
        }

        public void Refresh(MultiplayerClient.NetworkState state, string selfId)
        {
            var self = Array.Find(state.players, p => p.id == selfId);
            if (scoreText != null) scoreText.text = self == null ? "" : $"BANKED  {self.banked}\nCARRYING  {self.carried}";
            var remaining = state.phase == "playing" ? Math.Max(0, state.endsAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000 : 0;
            if (phaseText != null) phaseText.text = state.phase == "playing" ? $"{remaining / 60:00}:{remaining % 60:00}" : state.phase.ToUpperInvariant();
            if (leaderboardText != null)
            {
                var ranked = state.players.OrderByDescending(p => p.banked).ThenBy(p => p.id).Select((p, i) => $"{i + 1}. {(p.id == selfId ? "YOU" : Short(p.id))}   {p.banked}{(p.alive ? "" : "  OUT")}");
                leaderboardText.text = "BANK LEADERS\n" + string.Join("\n", ranked);
                if (state.phase == "finished") leaderboardText.text += string.IsNullOrEmpty(state.winnerId) ? "\n\nDRAW" : $"\n\n{(state.winnerId == selfId ? "YOU WIN" : Short(state.winnerId) + " WINS")}";
            }
        }

        private static string Short(string id) => string.IsNullOrEmpty(id) ? "PLAYER" : id.Substring(0, Math.Min(5, id.Length)).ToUpperInvariant();
        private static TMP_Text Text(string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>(); text.fontSize = fontSize; text.alignment = alignment; text.color = Color.white; text.font = Resources.Load<TMP_FontAsset>("CartoonUI/Fredoka SDF"); text.enableWordWrapping = false; return text;
        }
    }
}
