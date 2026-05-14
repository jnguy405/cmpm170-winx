/* ------------------------------------------------------------
- Jenalee Nguyen - 2026
- jnguy405@ucsc.edu
---------------------------------------------------------------   
   Function Summary (grouped by TAG)

   MAP:
   - ValidateHeightParameters()
   - GetGroundAdjustedPosition(Vector3)
   - GetGroundAdjustedPosition(Vector3, out bool)

   CLUSTER:
   - ValidateClusterParameters()
   - GenerateClusterCenters(int)
   - DistributeObjectsToClusters(int)
   - GenerateClusterPositions(Vector3, int, float)

   OBJECT:
   - SpawnClusteredObjects()
   - SpawnObject()
   - SpawnObjectAtPosition(Vector3)
   - ClearPreviousSpawns()

   DEBUG:
   - OnDrawGizmosSelected()
   - UpdateExcludedLayerMask()

   UTIL / MISC:
   - InitializeRandom()
   - InitializeLayerExclusions()
   - IsPointInBox(Vector3, Vector3, Vector3)
 ------------------------------------------------------------- */

using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using npcAI;
using UnityEngine.Serialization;

public class Spawner : MonoBehaviour {

    [Header("NPC models")]
    [Tooltip("Optional. When set, prefabs are picked randomly from this list (NavMeshAgent + NPC — use NPC Model Registry / NPCModelSetup). If empty, SpawnerConfig prefabs are used.")]
    [FormerlySerializedAs("enemyModelRegistry")]
    [SerializeField] NPCModelRegistry npcModelRegistry;

    // Dropdown to display spawner configuration in inspector
    [Header("Spawner Config")]
    public SpawnerConfig config = new SpawnerConfig();

    // Initialize variables
    private System.Random random;
    private List<Vector3> clusterCenters = new List<Vector3>();                 // List of Vector3 to hold the centers of clusters
    private List<List<Vector3>> clusterPositions = new List<List<Vector3>>();   // List of List of Vector3 to hold positions within each cluster
    private List<GameObject> spawnedObjects = new List<GameObject>();           // List of GameObject to keep track of spawned objects
    private int excludedLayerMask = 0;                                          // Integer bitmask for excluded layers
    private bool areaConstraintViolated = false;                                // Boolean flag for area constraint violations

    // Start the Unity lifecycle
    // Calls random and layer exclusion initializers for setup
    void Start() {
        InitializeRandom();
        InitializeLayerExclusions();
        SpawnClusteredObjects();
    }

    [ContextMenu("Spawn Clustered Objects")]
    void ContextMenuSpawnClusteredObjects() => SpawnClusteredObjects();

    // Validate configuration on changes in the inspector
    void OnValidate() {
        if (config == null) return;
        InitializeLayerExclusions();
        ValidateHeightParameters();
        ValidateClusterParameters();
    }

    // MAP: Checks if height parameters are valid and adjusts them if necessary
    private void ValidateHeightParameters() {
        if (config == null) return;
        if (config.minHeightAboveGround > config.maxHeightAboveGround) {
            config.maxHeightAboveGround = config.minHeightAboveGround;
        }

        if (config.maxHeightAboveGround < config.minHeightAboveGround) {
            config.minHeightAboveGround = config.maxHeightAboveGround;
        }
    }

    // CLUSTER: Keeps cluster count and spawn total in valid ranges (fixed-count spawn, no wave randomization)
    private void ValidateClusterParameters() {
        if (config == null) return;
        config.totalObjects = Mathf.Max(1, config.totalObjects);
        config.clusterCount = Mathf.Max(1, config.clusterCount);
    }

    // Initializes the random seed based on configuration
    private void InitializeRandom() {
        if (config == null) return;
        // Set up random number generator
        if (config.useRandomSeed) {
            random = new System.Random(config.randomSeed);
        }
        else {
            // Use a time-based seed for randomness
            random = new System.Random(Guid.NewGuid().GetHashCode());
        }
    }

    // Initializes layer exclusions based on configuration
    private void InitializeLayerExclusions() {
        if (config == null) return;
        // Initializes layerExclusions array if null or incorrect size
        if (config.layerExclusions == null || config.layerExclusions.Length != 32) {
            config.layerExclusions = new LayerExclusion[32];
        }

        // Populate layer names and indices
        for (int i = 0; i < 32; i++) {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName)) {
                layerName = $"Layer {i} (Unused)";
            }
            // Create new LayerExclusion if null
            if (config.layerExclusions[i] == null) {
                config.layerExclusions[i] = new LayerExclusion();
            }
            // Set layer name and index
            config.layerExclusions[i].layerName = layerName;
            config.layerExclusions[i].layerIndex = i;
        }

        UpdateExcludedLayerMask();
    }

    // Updates the excluded layer mask based on current exclusions
    private void UpdateExcludedLayerMask() {
        excludedLayerMask = 0;

        if (config == null || !config.useLayerExclusion || config.layerExclusions == null) return;

        // Build the excluded layer mask - each excluded layer sets its bit in the mask
        foreach (var layerExclusion in config.layerExclusions) {
            if (layerExclusion != null && layerExclusion.excludeFromSpawn) {
                excludedLayerMask |= 1 << layerExclusion.layerIndex;
            }
        }
    }

    // CLUSTER: Main method to spawn clustered objects based on configuration
    public void SpawnClusteredObjects() {
        if (config == null) {
            Debug.LogError("Spawner: No SpawnerConfig assigned.");
            return;
        }
        // Reset previous state
        ClearPreviousSpawns();
        clusterCenters.Clear();
        clusterPositions.Clear();
        areaConstraintViolated = false;

        // Initialize random if not already done
        if (random == null) InitializeRandom();
        UpdateExcludedLayerMask();
        ValidateHeightParameters();
        ValidateClusterParameters();

        int clusterCount = Mathf.Min(config.totalObjects, config.clusterCount);
        clusterCount = Mathf.Max(1, clusterCount);

        GenerateClusterCenters(clusterCount);

        // DEBUG: Warn if area constraints are violated
        if (areaConstraintViolated) {
            Debug.LogWarning($"Placement area is too small for {clusterCount} clusters with minimum distance {config.minClusterDistance}. " +
                           $"Consider: (1) Increasing Placement Area Size, (2) Decreasing Cluster Base Radius, " +
                           $"(3) Reducing Cluster Count, or (4) Decreasing Min Cluster Distance.");
        }

        List<int> objectsPerClusterList = DistributeObjectsToClusters(clusterCount);
        int totalSpawned = 0;

        // Spawn objects for each cluster - gets positions and instantiates objects
        // Positions are stored and used for gizmo visualization
        for (int i = 0; i < objectsPerClusterList.Count; i++) {
            float radius = config.clusterBaseRadius * (1 + (float)random.NextDouble() * config.clusterRadiusVariability);
            List<Vector3> positions = GenerateClusterPositions(
                clusterCenters[i],
                objectsPerClusterList[i],
                radius
            );

            clusterPositions.Add(positions);

            foreach (Vector3 position in positions) {
                SpawnObjectAtPosition(position);
                totalSpawned++;
            }
        }
        // DEBUG: Displays total spawned and cluster count
        Debug.Log($"Spawned {totalSpawned} objects in {objectsPerClusterList.Count} clusters.");
    }

    // OBJECT: Spawns a single object at a random position within a defined range
    public void SpawnObject() {
        int spawnPointX = UnityEngine.Random.Range(-10, 10);
        int spawnPointZ = UnityEngine.Random.Range(-10, 10);

        Vector3 groundPosition = GetGroundAdjustedPosition(new Vector3(spawnPointX, 0, spawnPointZ));
        groundPosition.y += UnityEngine.Random.Range(10, 20);
        SpawnObjectAtPosition(groundPosition);
    }

    // OBJECT: NavMesh enemies + dynamic Rigidbodies tunnel through floors; prefabs often still have an RB — make kinematic or remove and snap to NavMesh.
    private void SpawnObjectAtPosition(Vector3 position) {
        if (config == null) {
            Debug.LogError("Spawner: No SpawnerConfig assigned.");
            return;
        }
        if (random == null) InitializeRandom();

        GameObject prefab = null;
        if (npcModelRegistry != null)
            prefab = npcModelRegistry.GetRandomSpawnPrefab(random);
        if (prefab == null)
            prefab = config.GetRandomSpawnPrefab(random);
        if (prefab == null) {
            Debug.LogError("No spawn prefab: assign NPC Model Registry entries (and/or SpawnerConfig myObject / optionalSpawnPrefabs).");
            return;
        }

        GameObject spawnedObj = Instantiate(prefab, position, Quaternion.identity);
        NPCModelSetup.ApplyToRoot(spawnedObj);

        var navAgent = spawnedObj.GetComponentInChildren<NavMeshAgent>(true);
        if (navAgent != null)
        {
            foreach (var rb in spawnedObj.GetComponentsInChildren<Rigidbody>(true))
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            navAgent.enabled = true;

            float sampleRadius = Mathf.Max(6f, navAgent.height * 2f + navAgent.baseOffset);
            Vector3 warpPos = position;
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, sampleRadius, navAgent.areaMask))
            {
                if (!NavMesh.SamplePosition(position, out hit, sampleRadius * 2f, NavMesh.AllAreas))
                    Debug.LogWarning($"Spawner: No NavMesh within {sampleRadius}m of {position}. NPC may fall.");
                else
                    warpPos = hit.position;
            }
            else
                warpPos = hit.position;

            // NavMesh position is the agent (feet) point. If the agent is on a child, moving only
            // the root to warpPos misaligns colliders vs floor and Rigidbodies can tunnel through.
            Transform root = spawnedObj.transform;
            Transform agentT = navAgent.transform;
            if (agentT == root)
            {
                root.position = warpPos;
            }
            else
            {
                root.position += warpPos - agentT.position;
            }
            Physics.SyncTransforms();

            navAgent.nextPosition = navAgent.transform.position;
            navAgent.Warp(warpPos);

            StartCoroutine(ResyncNavAgentNextFrame(navAgent, warpPos));
        }
        else if (spawnedObj.GetComponentInChildren<CharacterController>(true) == null
                 && spawnedObj.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = spawnedObj.AddComponent<Rigidbody>();
            rb.useGravity = true;
        }

        spawnedObjects.Add(spawnedObj);
    }

    IEnumerator ResyncNavAgentNextFrame(NavMeshAgent agent, Vector3 warpPos)
    {
        yield return new WaitForFixedUpdate();
        if (agent != null && agent.gameObject != null && agent.isActiveAndEnabled)
        {
            agent.Warp(warpPos);
            if (!agent.isOnNavMesh)
                Debug.LogWarning($"Spawner: Agent still off NavMesh after Warp at {warpPos}. Bake NavMesh under spawn area or widen placement.");
        }
    }

    // CLEANUP: Clears previously spawned objects if configured to do so
    public void ClearPreviousSpawns() {
        if (config == null || !config.destroyPreviousSpawns) return;

        foreach (GameObject obj in spawnedObjects) {
            if (obj != null) {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    // MAP: Adjusts the given position to align with the ground using raycasting
    private Vector3 GetGroundAdjustedPosition(Vector3 originalPosition) {
        bool hitExcluded;
        return GetGroundAdjustedPosition(originalPosition, out hitExcluded);
    }

    // MAP: Adjusts based on ground raycast, checks for excluded layers
    private Vector3 GetGroundAdjustedPosition(Vector3 originalPosition, out bool hitExcludedLayer) {
        if (config == null) {
            hitExcludedLayer = true;
            return originalPosition;
        }
        Vector3 raycastStart = originalPosition + Vector3.up * 100f;
        hitExcludedLayer = false;

        if (config.showGroundRays) {
            Debug.DrawRay(raycastStart, Vector3.down * 200f, config.groundRayColor, 2f);
        }

        RaycastHit hit;
        // Checks for ground hit using raycast and returns the adjusted position
        if (Physics.Raycast(raycastStart, Vector3.down, out hit, 200f, ~0)) {
            int hitLayerMask = 1 << hit.collider.gameObject.layer;

            if (config.useLayerExclusion && (hitLayerMask & excludedLayerMask) != 0) {
                hitExcludedLayer = true;
                return originalPosition;
            }

            if ((hitLayerMask & config.groundLayer) == 0) {
                hitExcludedLayer = true;
                return originalPosition;
            }

            float randomHeight = (float)random.NextDouble();
            float heightAboveGround = config.minHeightAboveGround +
                                    randomHeight * (config.maxHeightAboveGround - config.minHeightAboveGround);

            Vector3 adjustedPosition = hit.point + Vector3.up * heightAboveGround;
            adjustedPosition.x = originalPosition.x;
            adjustedPosition.z = originalPosition.z;

            if (config.showGroundRays) {
                Debug.DrawLine(hit.point, adjustedPosition, Color.green, 2f);
            }

            return adjustedPosition;
        }
        else {
            // DEBUG: Warn if no ground detected and use fallback height
            Debug.LogWarning($"No ground detected at position {originalPosition}. Using fallback height.");

            Vector3 fallbackPosition = originalPosition;
            fallbackPosition.y = config.fallbackSpawnHeight;

            return fallbackPosition;
        }
    }

    // CLUSTER: Generates cluster centers (midpoints) while respecting exclusion zones and minimum distances
    private void GenerateClusterCenters(int clusterCount) {
        int maxAttempts = 100;

        for (int i = 0; i < clusterCount; i++) {
            Vector3 candidate;
            bool validPosition;
            int attempts = 0;

            // Try to find a valid cluster center position
            do {
                validPosition = true;
                attempts++;

                float x = (float)(random.NextDouble() * 2 - 1) * config.placementAreaSize.x / 2 + config.placementCenter.x;
                float y = config.placementCenter.y;
                float z = (float)(random.NextDouble() * 2 - 1) * config.placementAreaSize.z / 2 + config.placementCenter.z;
                candidate = new Vector3(x, y, z);

                // Check against exclusion zones
                foreach (var zone in config.exclusionZones) {
                    if (IsPointInBox(candidate, zone.center, zone.size)) {
                        validPosition = false;
                        break;
                    }
                }

                // Check minimum distance from existing cluster centers
                if (validPosition && clusterCenters.Count > 0) {
                    foreach (Vector3 existingCenter in clusterCenters) {
                        float distance = Vector2.Distance(
                            new Vector2(candidate.x, candidate.z),
                            new Vector2(existingCenter.x, existingCenter.z));

                        if (distance < config.minClusterDistance) {
                            validPosition = false;
                            break;
                        }
                    }
                }

                // If too many attempts, accept the position and flag constraint violation
                if (attempts > maxAttempts) {
                    areaConstraintViolated = true;
                    validPosition = true;
                }
            }
            while (!validPosition);

            bool hitExcluded;
            candidate = GetGroundAdjustedPosition(candidate, out hitExcluded);

            if (hitExcluded && attempts < maxAttempts) {
                i--; // Retry this cluster
                continue;
            }

            clusterCenters.Add(candidate);
        }
    }

    // CLUSTER: Splits totalObjects evenly across clusterCount (remainder to first clusters — fixed total, no waves)
    private List<int> DistributeObjectsToClusters(int clusterCount) {
        var distribution = new List<int>(clusterCount);
        int n = config.totalObjects;
        int k = Mathf.Max(1, clusterCount);
        int baseCount = n / k;
        int remainder = n % k;
        for (int i = 0; i < k; i++)
            distribution.Add(baseCount + (i < remainder ? 1 : 0));
        return distribution;
    }

    // CLUSTER: Generates positions within a cluster around a center point (based on center, count, radius)
    private List<Vector3> GenerateClusterPositions(Vector3 center, int count, float radius) {
        List<Vector3> positions = new List<Vector3>();
        int maxAttemptsPerPosition = 20;

        for (int i = 0; i < count; i++) {
            Vector3 position;
            bool validPosition;
            int attempts = 0;

            // Gets a valid position within the cluster radius
            do {
                attempts++;
                validPosition = true;

                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                position = center + randomOffset;

                // Generates position within placement area size bounds
                position.x = Mathf.Clamp(position.x,
                    config.placementCenter.x - config.placementAreaSize.x / 2,
                    config.placementCenter.x + config.placementAreaSize.x / 2);
                position.z = Mathf.Clamp(position.z,
                    config.placementCenter.z - config.placementAreaSize.z / 2,
                    config.placementCenter.z + config.placementAreaSize.z / 2);

                bool hitExcluded;
                Vector3 groundPos = GetGroundAdjustedPosition(position, out hitExcluded);

                if (hitExcluded) {
                    validPosition = false;
                    if (attempts >= maxAttemptsPerPosition) {
                        // DEBUG: Warn if unable to find valid position in cluster after max attempts
                        Debug.LogWarning($"Could not find valid position in cluster, skipping object");
                        break;
                    }
                    continue;
                }

                position = groundPos;

            } while (!validPosition && attempts < maxAttemptsPerPosition);

            if (validPosition) {
                positions.Add(position);
            }
        }

        return positions;
    }

    // UTIL: Checks if a point is inside a box defined by center and size
    private bool IsPointInBox(Vector3 point, Vector3 boxCenter, Vector3 boxSize) {
        return Mathf.Abs(point.x - boxCenter.x) <= boxSize.x / 2 &&
               Mathf.Abs(point.y - boxCenter.y) <= boxSize.y / 2 &&
               Mathf.Abs(point.z - boxCenter.z) <= boxSize.z / 2;
    }

    // DEBUG: Visualizes placement area, exclusion zones, cluster centers, and object positions
    void OnDrawGizmosSelected() {
        if (config == null || !config.showDebugGizmos) return;

        Gizmos.color = config.placementAreaColor;
        Gizmos.DrawWireCube(config.placementCenter, config.placementAreaSize);

        Gizmos.color = config.exclusionZoneColor;
        foreach (var zone in config.exclusionZones) {
            Gizmos.DrawWireCube(zone.center, zone.size);
        }

        Gizmos.color = config.clusterCenterColor;
        foreach (Vector3 center in clusterCenters) {
            Gizmos.DrawSphere(center, 0.5f);
            Gizmos.DrawWireSphere(center, config.minClusterDistance);
        }

        foreach (List<Vector3> cluster in clusterPositions) {
            foreach (Vector3 pos in cluster) {
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }
    }
}