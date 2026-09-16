using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MazeZero.Editor
{
    public static class MazeZeroWebBuild
    {
        public static void Build()
        {
            var target = Environment.GetEnvironmentVariable("MAZE_ZERO_WEB_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(target)) throw new InvalidOperationException("MAZE_ZERO_WEB_BUILD_PATH is required.");
            Directory.CreateDirectory(target);
            var scenes = new[] { "Assets/Scenes/Lobby.unity", "Assets/Scenes/SampleScene.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = target,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var previousCompression = PlayerSettings.WebGL.compressionFormat;
            var previousFallback = PlayerSettings.WebGL.decompressionFallback;
            var previousCaching = PlayerSettings.WebGL.dataCaching;
            try
            {
                // The host supplies Content-Encoding: gzip, so mobile browsers can
                // decompress natively without Unity's extra JavaScript fallback.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.dataCaching = false;
                var result = BuildPipeline.BuildPlayer(options);
                if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception($"Maze Zero WebGL build failed: {result.summary.result} ({result.summary.totalErrors} errors)");
                Debug.Log($"Maze Zero WebGL build succeeded at {target}");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousCompression;
                PlayerSettings.WebGL.decompressionFallback = previousFallback;
                PlayerSettings.WebGL.dataCaching = previousCaching;
            }
        }
    }
}
