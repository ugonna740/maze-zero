using System;
using UnityEngine;

namespace MazeZero
{
    public static class NimiqRunContext
    {
        public static string Mode { get; private set; } = "practice";
        public static string Address { get; private set; }
        public static string RunId { get; private set; }
        public static string AuthToken { get; private set; }
        public static string Queue { get; private set; } = "public";
        public static string RoomCode { get; private set; }
        public static int StakeLuna { get; private set; }
        public static string ServerUrl { get; private set; }
        public static bool IsMultiplayer => Mode == "multiplayer" || Mode == "nim-arena";

        public static void UsePractice()
        {
            Mode = "practice";
            Address = null;
            RunId = null;
            AuthToken = null;
            Queue = "public"; RoomCode = null; StakeLuna = 0;
            ServerUrl = null;
        }

        public static bool Apply(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<LaunchPayload>(json);
                if (payload == null || string.IsNullOrWhiteSpace(payload.mode)) return false;
                Mode = payload.mode;
                Address = payload.address;
                RunId = payload.runId;
                AuthToken = payload.authToken;
                Queue = string.IsNullOrWhiteSpace(payload.queue) ? "public" : payload.queue;
                RoomCode = payload.roomCode;
                StakeLuna = payload.stakeLuna;
                ServerUrl = payload.serverUrl;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Invalid Nimiq launch payload: {exception.Message}");
                return false;
            }
        }

        [Serializable]
        private sealed class LaunchPayload
        {
            public string mode;
            public string address;
            public string runId;
            public string authToken;
            public string queue;
            public string roomCode;
            public int stakeLuna;
            public string serverUrl;
        }
    }
}
