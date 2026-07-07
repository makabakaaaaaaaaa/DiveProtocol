using System;
using System.Collections.Generic;
using System.Linq;

namespace DiveProtocol.Editor
{
    internal static class BuildSceneConfiguration
    {
        private const string _systemFolder = "Assets/_DiveProtocol/Scenes/System";
        private const string _levelsFolder = "Assets/_DiveProtocol/Scenes/Levels";

        internal static readonly string[] CoreBuildScenePaths =
        {
            $"{_systemFolder}/{SceneNames.Bootstrap}.unity",
            $"{_systemFolder}/{SceneNames.MainMenu}.unity",
            $"{_levelsFolder}/{SceneNames.Level01Drainage}.unity",
            $"{_levelsFolder}/{SceneNames.DemoLevel}.unity",
            $"{_systemFolder}/{SceneNames.Results}.unity"
        };

        internal static string[] ComposeBuildScenes(IEnumerable<string> requiredCorePaths, IEnumerable<string> existingPaths)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var path in requiredCorePaths.Where(IsValidScenePath))
            {
                if (seen.Add(path))
                {
                    result.Add(path);
                }
            }

            foreach (var path in existingPaths.Where(IsValidScenePath))
            {
                if (seen.Add(path))
                {
                    result.Add(path);
                }
            }

            return result.ToArray();
        }

        internal static bool StartsWithCoreScenes(IReadOnlyList<string> configuredPaths, IReadOnlyList<string> requiredCorePaths)
        {
            if (configuredPaths.Count < requiredCorePaths.Count)
            {
                return false;
            }

            for (var i = 0; i < requiredCorePaths.Count; i++)
            {
                if (!string.Equals(configuredPaths[i], requiredCorePaths[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidScenePath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }
    }
}
