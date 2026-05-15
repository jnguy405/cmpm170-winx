using UnityEngine;
using System;
using System.Collections.Generic;

public class DustSpawner : MonoBehaviour
{
    [Header("Spawner Config")]
    public SpawnerConfig config = new SpawnerConfig();

    private System.Random random;
    private readonly List<Vector3> clusterCenters = new List<Vector3>();
    private readonly List<List<Vector3>> clusterPositions = new List<List<Vector3>>();
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private int excludedLayerMask;
    private bool areaConstraintViolated;

    void Start()
    {
        InitializeRandom();
        InitializeLayerExclusions();
        SpawnClusteredObjects();
    }

    [ContextMenu("Spawn Clustered Objects")]
    void ContextMenuSpawnClusteredObjects() => SpawnClusteredObjects();

    void OnValidate()
    {
        if (config == null) return;
        InitializeLayerExclusions();
        ValidateHeightParameters();
        ValidateClusterParameters();
    }

    private void ValidateHeightParameters()
    {
        if (config == null) return;
        if (config.minHeightAboveGround > config.maxHeightAboveGround)
            config.maxHeightAboveGround = config.minHeightAboveGround;
        if (config.maxHeightAboveGround < config.minHeightAboveGround)
            config.minHeightAboveGround = config.maxHeightAboveGround;
    }

    private void ValidateClusterParameters()
    {
        if (config == null) return;
        config.totalObjects = Mathf.Max(1, config.totalObjects);
        config.clusterCount = Mathf.Max(1, config.clusterCount);
    }

    private void InitializeRandom()
    {
        if (config == null) return;
        random = config.useRandomSeed
            ? new System.Random(config.randomSeed)
            : new System.Random(Guid.NewGuid().GetHashCode());
    }

    private void InitializeLayerExclusions()
    {
        if (config == null) return;
        if (config.layerExclusions == null || config.layerExclusions.Length != 32)
            config.layerExclusions = new LayerExclusion[32];

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
                layerName = $"Layer {i} (Unused)";

            if (config.layerExclusions[i] == null)
                config.layerExclusions[i] = new LayerExclusion();

            config.layerExclusions[i].layerName = layerName;
            config.layerExclusions[i].layerIndex = i;
        }

        UpdateExcludedLayerMask();
    }

    private void UpdateExcludedLayerMask()
    {
        excludedLayerMask = 0;
        if (config == null || !config.useLayerExclusion || config.layerExclusions == null) return;

        foreach (var layerExclusion in config.layerExclusions)
        {
            if (layerExclusion != null && layerExclusion.excludeFromSpawn)
                excludedLayerMask |= 1 << layerExclusion.layerIndex;
        }
    }

    public void SpawnClusteredObjects()
    {
        if (config == null)
        {
            Debug.LogError("DustSpawner: No SpawnerConfig assigned.");
            return;
        }

        ClearPreviousSpawns();
        clusterCenters.Clear();
        clusterPositions.Clear();
        areaConstraintViolated = false;

        if (random == null) InitializeRandom();
        UpdateExcludedLayerMask();
        ValidateHeightParameters();
        ValidateClusterParameters();

        int clusterCount = Mathf.Min(config.totalObjects, config.clusterCount);
        clusterCount = Mathf.Max(1, clusterCount);

        GenerateClusterCenters(clusterCount);

        if (areaConstraintViolated)
        {
            Debug.LogWarning(
                $"DustSpawner: Placement area is too small for {clusterCount} clusters with minimum distance {config.minClusterDistance}.");
        }

        List<int> objectsPerClusterList = DistributeObjectsToClusters(clusterCount);
        int totalSpawned = 0;

        for (int i = 0; i < objectsPerClusterList.Count; i++)
        {
            float radius = config.clusterBaseRadius * (1 + (float)random.NextDouble() * config.clusterRadiusVariability);
            List<Vector3> positions = GenerateClusterPositions(clusterCenters[i], objectsPerClusterList[i], radius);
            clusterPositions.Add(positions);

            foreach (Vector3 position in positions)
            {
                SpawnObjectAtPosition(position);
                totalSpawned++;
            }
        }

        Debug.Log($"DustSpawner: Spawned {totalSpawned} objects in {objectsPerClusterList.Count} clusters.");
    }

    public void HandleCollected(GameObject dust)
    {
        if (dust == null)
            return;

        spawnedObjects.Remove(dust);
        Destroy(dust);
        PruneDestroyedSpawns();
        TrySpawnReplacement();
    }

    private void PruneDestroyedSpawns()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] == null)
                spawnedObjects.RemoveAt(i);
        }
    }

    private int GetActiveSpawnCount()
    {
        PruneDestroyedSpawns();
        return spawnedObjects.Count;
    }

    private bool TrySpawnReplacement()
    {
        if (config == null)
            return false;

        if (GetActiveSpawnCount() >= config.totalObjects)
            return false;

        if (random == null)
            InitializeRandom();

        if (!TryFindReplacementPosition(out Vector3 position))
        {
            Debug.LogWarning("DustSpawner: Could not find a valid replacement spawn position.");
            return false;
        }

        SpawnObjectAtPosition(position);
        return true;
    }

    private bool TryFindReplacementPosition(out Vector3 position)
    {
        position = default;
        if (config == null)
            return false;

        if (clusterCenters.Count == 0)
        {
            int clusterCount = Mathf.Min(config.totalObjects, config.clusterCount);
            clusterCount = Mathf.Max(1, clusterCount);
            GenerateClusterCenters(clusterCount);
        }

        if (clusterCenters.Count == 0)
            return false;

        const int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int clusterIndex = random.Next(0, clusterCenters.Count);
            float radius = config.clusterBaseRadius * (1 + (float)random.NextDouble() * config.clusterRadiusVariability);
            List<Vector3> positions = GenerateClusterPositions(clusterCenters[clusterIndex], 1, radius);
            if (positions.Count == 0)
                continue;

            position = positions[0];
            return true;
        }

        return false;
    }

    private void SpawnObjectAtPosition(Vector3 position)
    {
        if (config == null)
        {
            Debug.LogError("DustSpawner: No SpawnerConfig assigned.");
            return;
        }

        if (config.myObject == null)
        {
            Debug.LogError("DustSpawner: Assign the dust prefab to SpawnerConfig.myObject.");
            return;
        }

        GameObject spawnedObj = Instantiate(config.myObject, position, Quaternion.identity);
        DustPile dustPile = spawnedObj.GetComponent<DustPile>();
        if (dustPile != null)
            dustPile.SetSpawner(this);

        spawnedObjects.Add(spawnedObj);
    }

    public void ClearPreviousSpawns()
    {
        if (config == null || !config.destroyPreviousSpawns) return;

        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedObjects.Clear();
    }

    private Vector3 GetGroundAdjustedPosition(Vector3 originalPosition, out bool hitExcludedLayer)
    {
        if (config == null)
        {
            hitExcludedLayer = true;
            return originalPosition;
        }

        Vector3 raycastStart = originalPosition + Vector3.up * 100f;
        hitExcludedLayer = false;

        if (config.showGroundRays)
            Debug.DrawRay(raycastStart, Vector3.down * 200f, config.groundRayColor, 2f);

        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, 200f, ~0))
        {
            int hitLayerMask = 1 << hit.collider.gameObject.layer;

            if (config.useLayerExclusion && (hitLayerMask & excludedLayerMask) != 0)
            {
                hitExcludedLayer = true;
                return originalPosition;
            }

            if ((hitLayerMask & config.groundLayer) == 0)
            {
                hitExcludedLayer = true;
                return originalPosition;
            }

            float randomHeight = (float)random.NextDouble();
            float heightAboveGround = config.minHeightAboveGround +
                randomHeight * (config.maxHeightAboveGround - config.minHeightAboveGround);

            Vector3 adjustedPosition = hit.point + Vector3.up * heightAboveGround;
            adjustedPosition.x = originalPosition.x;
            adjustedPosition.z = originalPosition.z;

            if (config.showGroundRays)
                Debug.DrawLine(hit.point, adjustedPosition, Color.green, 2f);

            return adjustedPosition;
        }

        Debug.LogWarning($"DustSpawner: No ground detected at position {originalPosition}. Using fallback height.");
        Vector3 fallbackPosition = originalPosition;
        fallbackPosition.y = config.fallbackSpawnHeight;
        return fallbackPosition;
    }

    private void GenerateClusterCenters(int clusterCount)
    {
        const int maxAttempts = 100;

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 candidate;
            bool validPosition;
            int attempts = 0;

            do
            {
                validPosition = true;
                attempts++;

                float x = (float)(random.NextDouble() * 2 - 1) * config.placementAreaSize.x / 2 + config.placementCenter.x;
                float y = config.placementCenter.y;
                float z = (float)(random.NextDouble() * 2 - 1) * config.placementAreaSize.z / 2 + config.placementCenter.z;
                candidate = new Vector3(x, y, z);

                foreach (var zone in config.exclusionZones)
                {
                    if (IsPointInBox(candidate, zone.center, zone.size))
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (validPosition && clusterCenters.Count > 0)
                {
                    foreach (Vector3 existingCenter in clusterCenters)
                    {
                        float distance = Vector2.Distance(
                            new Vector2(candidate.x, candidate.z),
                            new Vector2(existingCenter.x, existingCenter.z));

                        if (distance < config.minClusterDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                }

                if (attempts > maxAttempts)
                {
                    areaConstraintViolated = true;
                    validPosition = true;
                }
            }
            while (!validPosition);

            candidate = GetGroundAdjustedPosition(candidate, out bool hitExcluded);
            if (hitExcluded && attempts < maxAttempts)
            {
                i--;
                continue;
            }

            clusterCenters.Add(candidate);
        }
    }

    private List<int> DistributeObjectsToClusters(int clusterCount)
    {
        var distribution = new List<int>(clusterCount);
        int n = config.totalObjects;
        int k = Mathf.Max(1, clusterCount);
        int baseCount = n / k;
        int remainder = n % k;
        for (int i = 0; i < k; i++)
            distribution.Add(baseCount + (i < remainder ? 1 : 0));
        return distribution;
    }

    private List<Vector3> GenerateClusterPositions(Vector3 center, int count, float radius)
    {
        var positions = new List<Vector3>();
        const int maxAttemptsPerPosition = 20;

        for (int i = 0; i < count; i++)
        {
            Vector3 position;
            bool validPosition;
            int attempts = 0;

            do
            {
                attempts++;
                validPosition = true;

                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                position = center + randomOffset;

                position.x = Mathf.Clamp(position.x,
                    config.placementCenter.x - config.placementAreaSize.x / 2,
                    config.placementCenter.x + config.placementAreaSize.x / 2);
                position.z = Mathf.Clamp(position.z,
                    config.placementCenter.z - config.placementAreaSize.z / 2,
                    config.placementCenter.z + config.placementAreaSize.z / 2);

                Vector3 groundPos = GetGroundAdjustedPosition(position, out bool hitExcluded);
                if (hitExcluded)
                {
                    validPosition = false;
                    if (attempts >= maxAttemptsPerPosition)
                    {
                        Debug.LogWarning("DustSpawner: Could not find valid position in cluster, skipping object.");
                        break;
                    }
                    continue;
                }

                position = groundPos;
            }
            while (!validPosition && attempts < maxAttemptsPerPosition);

            if (validPosition)
                positions.Add(position);
        }

        return positions;
    }

    private static bool IsPointInBox(Vector3 point, Vector3 boxCenter, Vector3 boxSize)
    {
        return Mathf.Abs(point.x - boxCenter.x) <= boxSize.x / 2 &&
               Mathf.Abs(point.y - boxCenter.y) <= boxSize.y / 2 &&
               Mathf.Abs(point.z - boxCenter.z) <= boxSize.z / 2;
    }

    void OnDrawGizmosSelected()
    {
        if (config == null || !config.showDebugGizmos) return;

        Gizmos.color = config.placementAreaColor;
        Gizmos.DrawWireCube(config.placementCenter, config.placementAreaSize);

        Gizmos.color = config.exclusionZoneColor;
        foreach (var zone in config.exclusionZones)
            Gizmos.DrawWireCube(zone.center, zone.size);

        Gizmos.color = config.clusterCenterColor;
        foreach (Vector3 center in clusterCenters)
        {
            Gizmos.DrawSphere(center, 0.5f);
            Gizmos.DrawWireSphere(center, config.minClusterDistance);
        }

        foreach (List<Vector3> cluster in clusterPositions)
        {
            foreach (Vector3 pos in cluster)
                Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
}
