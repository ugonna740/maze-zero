using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MazeZero
{
    public sealed class LobbyController : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void MazeZeroRequestMode(string mode);
#endif
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button eventsButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string gameplayScene = "SampleScene";

        private void Awake()
        {
            Bind(singlePlayerButton, OpenSinglePlayer);
            Bind(multiplayerButton, SelectMultiplayer);
            Bind(eventsButton, SelectEvents);
            if (statusText != null) statusText.text = "SELECT A MODE";
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        public void OpenSinglePlayer()
        {
            NimiqRunContext.UsePractice();
            if (statusText != null) statusText.text = "GENERATING SOLO RUN...";
            SceneManager.LoadScene(gameplayScene);
        }

        public void SelectMultiplayer()
        {
            RequestNimiqMode("multiplayer", "CONNECTING MULTIPLAYER...");
        }

        public void SelectEvents()
        {
            RequestNimiqMode("nim-arena", "CONNECTING NIM ARENA...");
        }

        public void OnNimiqModeAuthorized(string json)
        {
            if (!NimiqRunContext.Apply(json))
            {
                if (statusText != null) statusText.text = "NIMIQ AUTHORIZATION FAILED";
                return;
            }
            if (statusText != null) statusText.text = "GENERATING VERIFIED RUN...";
            SceneManager.LoadScene(gameplayScene);
        }

        private void RequestNimiqMode(string mode, string message)
        {
            if (statusText != null) statusText.text = message;
#if UNITY_WEBGL && !UNITY_EDITOR
            MazeZeroRequestMode(mode);
#else
            if (statusText != null) statusText.text = $"{mode.ToUpperInvariant()} REQUIRES THE NIMIQ MINI APP";
#endif
        }
    }
}
