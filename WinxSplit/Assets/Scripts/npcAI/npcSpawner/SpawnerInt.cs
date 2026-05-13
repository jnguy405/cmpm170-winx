using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;

[System.Serializable]
// CLASS: Defines an exclusion zone with center, size, and name
public class ExclusionZone {
    public Vector3 center;                      // Center position of the exclusion zone
    public Vector3 size;                        // Size of the exclusion zone
    public string zoneName = "Exclusion Zone";  // Name identifier for the exclusion zone
}

[System.Serializable]
// CLASS: Defines layer exclusion settings
public class LayerExclusion {
    public string layerName;                // Name of the layer
    public bool excludeFromSpawn = false;   // Toggle to exclude this layer from spawning
    public int layerIndex;                  // Index of the layer
}

[System.Serializable]
// CLASS: Configuration settings for the spawner system
public class SpawnerConfig {
    
    // SECTION: Spawn Object
    // Drag and drop the object to be spawned here
    [Header("Spawn Object")]
    [Tooltip("Primary spawn prefab. If Optional Prefabs is empty, only this is used.")]
    public GameObject myObject;

    [Tooltip("Optional extra prefabs. Each spawn picks uniformly at random among myObject and all non-null entries. Leave empty for a single fixed prefab.")]
    public GameObject[] optionalSpawnPrefabs;

    // Picks a prefab for one spawn. Uses myObject alone if optionalSpawnPrefabs is null/empty; otherwise includes myObject in the pool when set.
    public GameObject GetRandomSpawnPrefab(System.Random rng) {
        int count = 0;
        if (myObject != null) count++;
        if (optionalSpawnPrefabs != null) {
            for (int i = 0; i < optionalSpawnPrefabs.Length; i++) {
                if (optionalSpawnPrefabs[i] != null) count++;
            }
        }
        if (count == 0) return null;

        int pick = rng.Next(0, count);
        if (myObject != null) {
            if (pick == 0) return myObject;
            pick--;
        }
        if (optionalSpawnPrefabs != null) {
            for (int i = 0; i < optionalSpawnPrefabs.Length; i++) {
                if (optionalSpawnPrefabs[i] == null) continue;
                if (pick == 0) return optionalSpawnPrefabs[i];
                pick--;
            }
        }
        return null;
    }

    // SECTION: Spawn Parameters
    [Header("Spawn Parameters")]
    [Tooltip("Exact number of NPCs to spawn (no waves or random totals).")]
    [Min(1)]
    public int totalObjects = 20;

    // SECTION: Cluster layout (spatial grouping only; count is fixed by totalObjects)
    [Header("Cluster layout")]
    [Tooltip("Number of cluster centers. totalObjects is split evenly across clusters (remainder goes to the first clusters).")]
    [Min(1)]
    [FormerlySerializedAs("fixedClusterCount")]
    public int clusterCount = 4;

    // SECTION: Spatial Constraints
    // Defines the placement area, minimum cluster distance, and cluster radius settings
    [Header("Spatial Constraints")]
    [Tooltip("Center of the placement area (Map)")]
    public Vector3 placementCenter = Vector3.zero;

    [Tooltip("Size of the placement area")]
    public Vector3 placementAreaSize = new Vector3(50, 10, 50);

    [Tooltip("Minimum distance between each cluster (from centers)")]
    [Range(1f, 50f)]
    public float minClusterDistance = 10f;

    [Tooltip("Cluster radius variability (0-1)")]
    [Range(0f, 1f)]
    public float clusterRadiusVariability = 0.3f;

    [Tooltip("Base radius between object placement within cluster")]
    [Range(0f, 10f)]
    public float clusterBaseRadius = 5f;

    // SECTION: Ground Detection
    // Settings for raycasting to detect ground height for spawning
    [Header("Ground Detection")]
    [Tooltip("Minimum height above ground. Objects will spawn at least this far above the ground.")]
    [Range(0.1f, 10f)]
    public float minHeightAboveGround = 1f;

    [Tooltip("Maximum height above ground. Objects will spawn no more than this far above the ground. Must be >= Min Height.")]
    [Range(0.1f, 50f)]
    public float maxHeightAboveGround = 5f;

    [Tooltip("Layer mask for ground detection. Only objects on these layers are considered valid ground.")]
    public LayerMask groundLayer = -1;

    [Tooltip("If no ground is found, objects are spawned at this fallback height. If disabled (layerMask is nothing), objects spawn at this height regardless.")]
    [Range(0f, 20f)]
    public float fallbackSpawnHeight = 10f;

    // SECTION: Exclusion Zones
    // List of exclusion zones where objects should not be spawned
    [Header("Exclusion Zones")]
    public List<ExclusionZone> exclusionZones = new List<ExclusionZone>();

    [Header("Layer Exclusion System")]
    [Tooltip("Toggle to enable layer-based exclusion")]
    public bool useLayerExclusion = true;

    [Tooltip("List of all layers with toggle for exclusion")]
    public LayerExclusion[] layerExclusions = new LayerExclusion[32];

    // SECTION: Randomization
    // Settings for random seed usage
    [Header("Randomization")]
    public int randomSeed = 0;
    public bool useRandomSeed = false;

    // SECTION: Debug Visualization
    // Settings for visualizing placement area, exclusion zones, and ground rays in the editor
    [Header("Debug Visualization")]
    public bool showDebugGizmos = true;
    public bool showGroundRays = true;
    public Color placementAreaColor = new Color(0, 1, 0, 0.2f);
    public Color clusterCenterColor = Color.red;
    public Color exclusionZoneColor = new Color(1, 0, 0, 0.3f);
    public Color groundRayColor = Color.blue;

    // SECTION: Object Management
    // Option to destroy previously spawned objects before spawning new ones
    [Header("Object Management")]
    public bool destroyPreviousSpawns = true;
}