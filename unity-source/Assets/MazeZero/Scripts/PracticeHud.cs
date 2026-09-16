using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class PracticeHud : MonoBehaviour
    {
        private const int CurrentDesignVersion = 4;
        private const string Pack = "CartoonUI/ChequeredInk/";
        [SerializeField] private int designVersion;
        [SerializeField] private TMP_Text timer, score, seed, message, notification;
        [Header("Result Text Boxes")]
        [SerializeField] private TMP_Text resultTitle, resultDetail, resultStats;
        [SerializeField] private Image timerGauge;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private HorizontalCompass horizontalCompass;

        private static readonly Color Ink = new Color32(43, 39, 56, 255);
        private static readonly Color Cream = new Color32(255, 245, 210, 255);
        private static readonly Color Teal = new Color32(38, 190, 177, 255);
        private static readonly Color Orange = new Color32(255, 157, 54, 255);
        private static readonly Color Red = new Color32(238, 73, 76, 255);
        private static TMP_FontAsset CartoonFont => Resources.Load<TMP_FontAsset>("CartoonUI/Fredoka SDF");

        public static PracticeHud Create()
        {
            var canvasObject = new GameObject("Practice HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var hud = canvasObject.AddComponent<PracticeHud>(); hud.RebuildVisuals(); return hud;
        }

        private void Awake()
        {
            // Never rebuild serialized UI on Play. Scene edits are authoritative.
            EnsureReferences();
            BindResultTextBoxes();
            BindRestartButton();
        }

        public void RebuildVisuals()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "On-screen Controls" || child.name == "MiniMap") continue;
                if (Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject);
            }
            designVersion = CurrentDesignVersion;

            var scoreCard = AddPanel("Orb Card", new Vector2(0, 1), new Vector2(24, -24), new Vector2(330, 102), Color.white, Teal, null, 4, Pack + "Blue/Panel");
            AddImage("Orb Icon", new Vector2(0, .5f), new Vector2(18, 0), new Vector2(62, 62), Color.white, scoreCard.transform, Pack + "Orange/Orb", true);
            score = AddText("Orb Score", "0 / 0", 34, TextAlignmentOptions.MidlineLeft, new Vector2(0, .5f), new Vector2(82, 4), new Vector2(220, 44), Ink, scoreCard.transform);
            AddText("Orb Caption", "MAZE ORBS", 18, TextAlignmentOptions.TopLeft, new Vector2(0, .5f), new Vector2(84, -26), new Vector2(210, 28), Teal, scoreCard.transform);

            var timerCard = AddPanel("Timer Card", new Vector2(.5f, 1), new Vector2(0, -18), new Vector2(360, 126), Color.white, Teal, null, 4, Pack + "Blue/Panel");
            AddText("Timer Caption", "TIME LEFT", 18, TextAlignmentOptions.Center, new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(200, 26), Ink, timerCard.transform);
            timer = AddText("Timer", "03:00", 54, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(0, 5), new Vector2(300, 62), Color.white, timerCard.transform);
            var gaugeTrack = AddImage("Timer Gauge Track", new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(292, 20), Color.white, timerCard.transform, Pack + "Blue/GaugeTrack");
            timerGauge = AddImage("Timer Gauge", new Vector2(0, .5f), new Vector2(5, 0), new Vector2(282, 14), Color.white, gaugeTrack.transform, Pack + "Orange/GaugeFill");
            timerGauge.type = Image.Type.Filled; timerGauge.fillMethod = Image.FillMethod.Horizontal; timerGauge.fillOrigin = 0;

            horizontalCompass = HorizontalCompass.Create(transform, CartoonFont);
            seed = AddText("Run Seed", "SEED 000000", 17, TextAlignmentOptions.Right, new Vector2(1, 1), new Vector2(-178, -34), new Vector2(360, 32), Cream);

            notification = AddText("Hit Notification", "", 24, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(32, -142), new Vector2(580, 70), Orange);
            messagePanel = AddPanel("Result Panel", new Vector2(.5f, .5f), Vector2.zero, new Vector2(760, 410), Color.white, Teal, null, 8, Pack + "Blue/Popup").gameObject;
            AddImage("Result Ribbon Art", new Vector2(.5f, 1), new Vector2(0, 24), new Vector2(360, 72), Color.white, messagePanel.transform, Pack + "Orange/Ribbon");
            AddText("Result Ribbon", "RUN COMPLETE", 24, TextAlignmentOptions.Center, new Vector2(.5f, 1), new Vector2(0, 25), new Vector2(310, 58), Color.white, messagePanel.transform, new Vector2(.5f, .5f));
            resultTitle = AddText("Result Title", "", 48, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(0, 88), new Vector2(670, 70), Ink, messagePanel.transform);
            resultDetail = AddText("Result Detail", "", 34, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(670, 58), Teal, messagePanel.transform);
            resultStats = AddText("Result Stats", "", 30, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(0, -42), new Vector2(670, 54), Ink, messagePanel.transform);
            message = resultTitle;
            restartButton = CreateRestartButton(); messagePanel.SetActive(false);
        }

        public bool ReplaceCompassWithLinear()
        {
            var oldCompass = transform.Find("Compass");
            var existingLinear = transform.Find("Horizontal Compass");
            if (existingLinear != null)
            {
                horizontalCompass = existingLinear.GetComponent<HorizontalCompass>();
                designVersion = CurrentDesignVersion;
                return false;
            }
            if (oldCompass != null)
            {
                if (Application.isPlaying) Destroy(oldCompass.gameObject);
                else DestroyImmediate(oldCompass.gameObject);
            }
            horizontalCompass = HorizontalCompass.Create(transform, CartoonFont);
            designVersion = CurrentDesignVersion;
            return true;
        }

        public void Refresh(float remaining, int collected, int total, int runSeed, float duration = 180f)
        {
            if (!EnsureReferences()) return;
            var seconds = Mathf.Max(0, Mathf.CeilToInt(remaining)); timer.text = $"{seconds / 60:00}:{seconds % 60:00}"; score.text = $"{collected} / {total}"; seed.text = $"SEED {runSeed}";
            timerGauge.fillAmount = duration <= 0 ? 0 : Mathf.Clamp01(remaining / duration);
            var urgent = remaining <= 10f; var warning = remaining <= 30f;
            timerGauge.color = urgent ? new Color(1f, .45f, .45f) : warning ? Color.white : new Color(.8f, 1f, 1f);
            timer.color = Color.white;
            timer.transform.localScale = urgent ? Vector3.one * (1f + Mathf.Sin(Time.time * 9f) * .04f) : Vector3.one;
        }

        public void BeginRun() { StopAllCoroutines(); if (!EnsureReferences()) return; ClearResultText(); notification.text = ""; messagePanel.SetActive(false); var controls = transform.Find("On-screen Controls"); if (controls != null) controls.gameObject.SetActive(true); }
        public void ShowResult(bool escaped, int orbs, float elapsed, string failureTitle = "DETONATED")
        {
            if (!EnsureReferences()) return; var controls = transform.Find("On-screen Controls"); if (controls != null) controls.gameObject.SetActive(false); messagePanel.SetActive(true); messagePanel.transform.SetAsLastSibling();
            if (resultTitle != null && resultDetail != null && resultStats != null)
            {
                resultTitle.text = escaped ? "YOU ESCAPED!" : failureTitle;
                resultDetail.text = escaped ? $"{orbs} ORBS SAVED" : "RUN FAILED";
                resultStats.text = escaped ? $"{elapsed:0.0} SECONDS" : $"{orbs} ORBS LOST";
                resultDetail.color = escaped ? Teal : Red;
            }
            else
            {
                message.text = escaped ? $"YOU ESCAPED!\n{orbs} ORBS SAVED\n{elapsed:0.0} SECONDS" : $"{failureTitle}\nRUN FAILED\n{orbs} ORBS LOST";
            }
        }
        public void ShowTrapWarning(float penalty) => ShowTrapWarning($"OUCH!  -{penalty:0} SECONDS");
        public void ShowTrapWarning(string warning) { if (EnsureReferences()) { StopAllCoroutines(); StartCoroutine(TrapWarningRoutine(warning)); } }
        private IEnumerator TrapWarningRoutine(string warning) { notification.text = warning; notification.color = Red; yield return new WaitForSeconds(1.2f); notification.text = ""; }

        private bool EnsureReferences()
        {
            if (timer != null && score != null && seed != null && message != null && notification != null && timerGauge != null && messagePanel != null && restartButton != null) return true;
            foreach (var label in GetComponentsInChildren<TMP_Text>(true)) switch (label.gameObject.name) { case "Timer": timer = label; break; case "Orb Score": score = label; break; case "Run Seed": seed = label; break; case "Result Message": message = label; break; case "Hit Notification": notification = label; break; }
            var panel = transform.Find("Result Panel"); if (panel != null) messagePanel = panel.gameObject;
            BindResultTextBoxes();
            var gauge = transform.Find("Timer Card/Timer Gauge Track/Timer Gauge"); if (gauge != null) timerGauge = gauge.GetComponent<Image>();
            var button = GetComponentInChildren<Button>(true); if (button != null) restartButton = button;
            return timer != null && score != null && seed != null && message != null && notification != null && timerGauge != null && messagePanel != null && restartButton != null;
        }

        private void BindResultTextBoxes()
        {
            if (messagePanel == null) return;
            var candidates = new System.Collections.Generic.List<TMP_Text>();
            foreach (var label in messagePanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.transform.IsChildOf(restartButton != null ? restartButton.transform : null)) continue;
                if (label.gameObject.name == "Result Ribbon") continue;
                candidates.Add(label);
            }
            candidates.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            if (candidates.Count >= 3)
            {
                resultTitle = candidates[0]; resultDetail = candidates[1]; resultStats = candidates[2];
                message = resultTitle;
            }
            else if (message == null && candidates.Count > 0) message = candidates[0];
        }

        private void ClearResultText()
        {
            if (resultTitle != null) resultTitle.text = "";
            if (resultDetail != null) resultDetail.text = "";
            if (resultStats != null) resultStats.text = "";
            if (message != null) message.text = "";
        }

        private Button CreateRestartButton()
        {
            var image = AddPanel("Restart Run Button", new Vector2(.5f, 0), new Vector2(0, 35), new Vector2(330, 76), Color.white, Ink, messagePanel.transform, 5, Pack + "Orange/ButtonIdle"); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var states = button.spriteState; states.highlightedSprite = Resources.Load<Sprite>(Pack + "Orange/ButtonHover"); states.pressedSprite = Resources.Load<Sprite>(Pack + "Orange/ButtonPressed"); states.selectedSprite = states.highlightedSprite; button.spriteState = states;
            button.transition = Selectable.Transition.SpriteSwap;
            restartButton = button;
            BindRestartButton();
            AddImage("Restart Icon", new Vector2(0, .5f), new Vector2(38, 0), new Vector2(42, 42), Color.white, image.transform, Pack + "White/Restart", true);
            AddText("Restart Label", "PLAY AGAIN", 29, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(22, 0), new Vector2(240, 58), Color.white, image.transform); return button;
        }

        private void BindRestartButton()
        {
            if (restartButton == null) return;
            // Generated listeners are not serialized with the scene. Rebind without
            // disturbing any persistent callbacks the user configured in Inspector.
            restartButton.onClick.RemoveListener(RestartRun);
            restartButton.onClick.AddListener(RestartRun);
            restartButton.interactable = true;
        }

        private void RestartRun()
        {
            var manager = PracticeRunManager.Instance;
            if (manager == null) manager = FindFirstObjectByType<PracticeRunManager>();
            if (manager != null) manager.RestartRun();
        }

        private Image AddPanel(string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Color outline, Transform parent = null, int thickness = 4, string resource = null)
        {
            var image = AddImage(name, anchor, offset, size, color, parent, resource);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            if (image.sprite == null) { var edge = image.gameObject.AddComponent<Outline>(); edge.effectColor = outline; edge.effectDistance = new Vector2(thickness, -thickness); }
            var shadow = image.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .25f); shadow.effectDistance = new Vector2(0, -7); return image;
        }
        private Image AddImage(string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Transform parent = null, string resource = null, bool preserveAspect = false)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); item.transform.SetParent(parent != null ? parent : transform, false); var rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size;
            var image = item.GetComponent<Image>(); image.sprite = string.IsNullOrEmpty(resource) ? null : Resources.Load<Sprite>(resource); image.color = color; image.preserveAspect = preserveAspect; image.raycastTarget = false; return image;
        }
        private TMP_Text AddText(string name, string value, float size, TextAlignmentOptions alignment, Vector2 anchor, Vector2 offset, Vector2 dimensions, Color color, Transform parent = null, Vector2? pivot = null, Color? background = null)
        {
            if (background.HasValue) AddPanel(name + " Background", anchor, offset, dimensions, background.Value, Ink, parent, 3);
            var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent != null ? parent : transform, false); var rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot ?? anchor; rect.anchoredPosition = offset; rect.sizeDelta = dimensions;
            var text = item.GetComponent<TextMeshProUGUI>(); text.font = CartoonFont; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = color; text.enableWordWrapping = true; text.raycastTarget = false; text.fontStyle = FontStyles.Bold; text.outlineColor = new Color32(43, 39, 56, 120); text.outlineWidth = .12f; return text;
        }
    }
}
