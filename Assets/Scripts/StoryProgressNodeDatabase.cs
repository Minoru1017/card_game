using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StoryProgressNodeDatabase
{
    public string mapId = "world_map_v1";
    public string displayName = "World Map";
    public StoryProgressNodeEntry[] nodes = Array.Empty<StoryProgressNodeEntry>();
    public StoryProgressEdgeEntry[] edges = Array.Empty<StoryProgressEdgeEntry>();
}

[Serializable]
public sealed class StoryProgressNodeEntry
{
    public string nodeId;
    public string stageCode;
    public string title;
    public string chapter;
    public string nodeType;
    public string region;
    public bool isTutorial;
    public bool isBoss;
    public float x;
    public float y;
    public string[] unlockRequiresAllOf = Array.Empty<string>();
    public string[] unlockRequiresAnyOf = Array.Empty<string>();
}

[Serializable]
public sealed class StoryProgressEdgeEntry
{
    public string fromNodeId;
    public string toNodeId;
    public string pathType;
}

/// <summary>
/// Loads world-map stage-node definitions from Resources/StoryProgressNodeDatabase.json.
/// </summary>
public static class StoryProgressNodeDatabaseLibrary
{
    public const string ResourcePath = "StoryProgressNodeDatabase";

    private static StoryProgressNodeDatabase cached;

    public static StoryProgressNodeDatabase Load(bool forceReload = false)
    {
        if (!forceReload && cached != null)
            return cached;

        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null || string.IsNullOrWhiteSpace(json.text))
        {
            cached = new StoryProgressNodeDatabase();
            return cached;
        }

        try
        {
            cached = JsonUtility.FromJson<StoryProgressNodeDatabase>(json.text) ?? new StoryProgressNodeDatabase();
        }
        catch
        {
            cached = new StoryProgressNodeDatabase();
        }

        return cached;
    }

    public static bool TryGetNode(string nodeId, out StoryProgressNodeEntry node)
    {
        node = null;
        if (string.IsNullOrWhiteSpace(nodeId))
            return false;

        StoryProgressNodeDatabase db = Load();
        if (db.nodes == null)
            return false;

        for (int i = 0; i < db.nodes.Length; i++)
        {
            StoryProgressNodeEntry candidate = db.nodes[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.nodeId))
                continue;
            if (!string.Equals(candidate.nodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                continue;

            node = candidate;
            return true;
        }

        return false;
    }

    public static IReadOnlyList<StoryProgressNodeEntry> GetAllNodes()
    {
        StoryProgressNodeDatabase db = Load();
        return db.nodes ?? Array.Empty<StoryProgressNodeEntry>();
    }

    public static IReadOnlyList<StoryProgressEdgeEntry> GetAllEdges()
    {
        StoryProgressNodeDatabase db = Load();
        return db.edges ?? Array.Empty<StoryProgressEdgeEntry>();
    }

#if UNITY_EDITOR
    public static void InvalidateCache() => cached = null;
#endif
}
