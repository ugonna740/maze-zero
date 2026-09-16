using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class LobbySceneBuilder
    {
        private const string LobbyPath = "Assets/Scenes/Lobby.unity";
        private const string GameplayPath = "Assets/Scenes/SampleScene.unity";
        private const string UiRoot = "Assets/MazeZero/Resources/CartoonUI/ChequeredInk/";
        private const string FontPath = "Assets/MazeZero/Resources/CartoonUI/Fredoka SDF.asset";
        private static TMP_FontAsset font;

        static LobbySceneBuilder() => EditorApplication.delayCall += BuildIfMissing;

        [MenuItem("Maze Zero/Lobby/Rebuild Lobby Scene")]
        public static void Rebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyPath) != null &&
                !EditorUtility.DisplayDialog("Rebuild Lobby", "This replaces the existing Lobby scene and its manual edits.", "Rebuild", "Cancel")) return;
            Build(false);
        }

        private static void BuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyPath) == null) Build(false);
            else
            {
                EnsureBuildSettings();
                ApplyCameraFraming();
            }
        }

        private static void Build(bool openWhenDone)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            BuildEnvironment();
            BuildInterface();
            EditorSceneManager.SaveScene(scene, LobbyPath);
            EnsureBuildSettings();

            if (!openWhenDone && previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
            if (!openWhenDone) EditorSceneManager.CloseScene(scene, true);
            else
            {
                foreach (var loaded in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray())
                    if (loaded != scene) EditorSceneManager.CloseScene(loaded, true);
                SceneManager.SetActiveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Maze Zero Lobby scene created with Single Player, Multiplayer and Events modes.");
        }

        private static void BuildEnvironment()
        {
            var cameraObject = new GameObject("Lobby Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 2.75f, -6.4f), Quaternion.Euler(7.5f, 0f, 0f));
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 36f; camera.clearFlags = CameraClearFlags.Skybox;

            var sunObject = new GameObject("Lobby Sun", typeof(Light));
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1f, .91f, .78f); sun.shadows = LightShadows.Soft;

            var fillObject = new GameObject("Character Fill Light", typeof(Light));
            fillObject.transform.position = new Vector3(-3f, 4f, -3f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Point; fill.intensity = 2.2f; fill.range = 10f; fill.color = new Color(.35f, .7f, 1f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Lobby Ground"; ground.transform.position = new Vector3(0f, -.3f, 1.5f); ground.transform.localScale = new Vector3(18f, .5f, 16f);
            ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MazeZero/Resources/MazeZeroGroundTiles.mat");

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Industrial Backdrop"; back.transform.position = new Vector3(0f, 2.8f, 4.8f); back.transform.localScale = new Vector3(18f, 6f, .35f);
            back.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MazeZero/Resources/MazeZeroIndustrialWall.mat");

            var podium = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            podium.name = "Player Podium"; podium.transform.position = new Vector3(0f, .12f, 1.2f); podium.transform.localScale = new Vector3(1.4f, .18f, 1.4f);
            podium.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MazeZero/Resources/MazeZeroIndustrialWall.mat");

            var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MazeZero/Resources/MazeZeroCharacter.prefab");
            if (characterPrefab != null)
            {
                var character = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
                character.name = "Lobby Player"; character.transform.position = new Vector3(0f, .3f, 1.2f); character.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private static void BuildInterface()
        {
            var canvasObject = new GameObject("Lobby UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LobbyController));
            SetUiLayer(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = .5f;

            AddText("Game Title", "MAZE//ZERO", 78f, new Vector2(.5f, 1f), new Vector2(0f, -42f), new Vector2(800f, 100f), Color.white, canvasObject.transform);
            AddText("Lobby Subtitle", "CHOOSE YOUR RUN", 27f, new Vector2(.5f, 1f), new Vector2(0f, -132f), new Vector2(520f, 48f), new Color32(255, 177, 64, 255), canvasObject.transform);

            var tray = AddImage("Mode Selection Tray", new Vector2(.5f, 0f), new Vector2(0f, 44f), new Vector2(1480f, 310f), LoadSprite("Blue/Panel.png"), Color.white, canvasObject.transform);
            tray.type = Image.Type.Sliced; tray.raycastTarget = false;

            var solo = AddModeCard("Single Player", "SOLO MAZE", "PLAY NOW", -470f, new Color32(38, 190, 177, 255), tray.transform);
            var multi = AddModeCard("Multiplayer", "MULTIPLAYER", "COMING NEXT", 0f, new Color32(81, 135, 235, 255), tray.transform);
            var eventsButton = AddModeCard("Events", "EVENTS", "COMING NEXT", 470f, new Color32(255, 157, 54, 255), tray.transform);
            var status = AddText("Mode Status", "SELECT A MODE", 21f, new Vector2(.5f, 0f), new Vector2(0f, 370f), new Vector2(900f, 48f), Color.white, canvasObject.transform);

            var controller = canvasObject.GetComponent<LobbyController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("singlePlayerButton").objectReferenceValue = solo;
            serialized.FindProperty("multiplayerButton").objectReferenceValue = multi;
            serialized.FindProperty("eventsButton").objectReferenceValue = eventsButton;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SetUiLayer(eventSystem);
        }

        private static Button AddModeCard(string name, string heading, string caption, float x, Color accent, Transform parent)
        {
            var card = AddImage(name + " Card", new Vector2(.5f, .5f), new Vector2(x, 0f), new Vector2(410f, 230f), LoadSprite("Blue/Popup.png"), Color.white, parent);
            card.type = Image.Type.Sliced;
            AddText(name + " Heading", heading, 34f, new Vector2(.5f, 1f), new Vector2(0f, -28f), new Vector2(350f, 55f), accent, card.transform);
            AddText(name + " Caption", caption, 19f, new Vector2(.5f, .5f), new Vector2(0f, -10f), new Vector2(330f, 40f), new Color32(43, 39, 56, 255), card.transform);
            var buttonImage = AddImage(name + " Button", new Vector2(.5f, 0f), new Vector2(0f, 22f), new Vector2(300f, 65f), LoadSprite("Orange/ButtonIdle.png"), Color.white, card.transform);
            buttonImage.type = Image.Type.Sliced;
            var button = buttonImage.gameObject.AddComponent<Button>(); button.targetGraphic = buttonImage;
            var sprites = button.spriteState; sprites.highlightedSprite = LoadSprite("Orange/ButtonHover.png"); sprites.pressedSprite = LoadSprite("Orange/ButtonPressed.png"); button.spriteState = sprites; button.transition = Selectable.Transition.SpriteSwap;
            AddText(name + " Button Label", name == "Single Player" ? "ENTER" : "SELECT", 25f, new Vector2(.5f, .5f), Vector2.zero, new Vector2(250f, 48f), Color.white, button.transform);
            return button;
        }

        private static Image AddImage(string name, Vector2 anchor, Vector2 position, Vector2 size, Sprite sprite, Color color, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); SetUiLayer(item); item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = item.GetComponent<Image>(); image.sprite = sprite; image.color = color; return image;
        }

        private static TMP_Text AddText(string name, string value, float size, Vector2 anchor, Vector2 position, Vector2 dimensions, Color color, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); SetUiLayer(item); item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = item.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.color = color; text.fontStyle = FontStyles.Bold; text.raycastTarget = false; text.outlineWidth = .12f; text.outlineColor = new Color32(25, 31, 48, 150); return text;
        }

        private static Sprite LoadSprite(string relativePath) => AssetDatabase.LoadAssetAtPath<Sprite>(UiRoot + relativePath);

        private static void SetUiLayer(GameObject item) => item.layer = LayerMask.NameToLayer("UI");

        private static void EnsureBuildSettings()
        {
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToList();
            paths.Remove(LobbyPath); paths.Remove(GameplayPath); paths.Insert(0, GameplayPath); paths.Insert(0, LobbyPath);
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private static void ApplyCameraFraming()
        {
            const string versionKey = "MazeZero.LobbyCameraFraming.v2";
            if (SessionState.GetBool(versionKey, false)) return;
            var scene = SceneManager.GetSceneByPath(LobbyPath);
            var openedForEdit = !scene.IsValid() || !scene.isLoaded;
            if (openedForEdit) scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Additive);
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.gameObject.name == "Lobby Camera");
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(new Vector3(0f, 2.75f, -6.4f), Quaternion.Euler(7.5f, 0f, 0f));
                camera.fieldOfView = 36f;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (openedForEdit) EditorSceneManager.CloseScene(scene, true);
            SessionState.SetBool(versionKey, true);
        }
    }
}
