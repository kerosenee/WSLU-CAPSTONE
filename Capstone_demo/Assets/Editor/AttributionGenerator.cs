using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AttributionGenerator
{
    const string PrefabFolder = "Assets/External Models/Prefabs";
    const string OutputPath = "ATTRIBUTIONS.md";

    [MenuItem("Tools/Generate Attributions")]
    static void Generate()
    {
        // Every asset referenced by the scenes enabled in Build Settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        HashSet<string> usedAssets = AssetDatabase.GetDependencies(scenes, true).ToHashSet();

        List<string> lines = new List<string> { "# Attributions", "" };
        List<string> unused = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!usedAssets.Contains(path))
            {
                unused.Add(path);
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AssetAttribution doc = prefab.GetComponent<AssetAttribution>();

            if (doc == null)
            {
                Debug.LogWarning($"{path} is used but has no AssetAttribution component.", prefab);
                continue;
            }

            lines.Add($"- \"{doc.title}\" by {doc.author} ({doc.sourceUrl}), {doc.license}");
        }

        File.WriteAllLines(OutputPath, lines);
        Debug.Log($"Wrote {OutputPath}. Unused external prefabs:\n{string.Join("\n", unused)}");
    }
}
