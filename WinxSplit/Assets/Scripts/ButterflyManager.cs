using UnityEngine;
using System.Linq;

public class ButterflyManager : MonoBehaviour {
    public ParticleSystem[] butterflySystems;

    public TextAsset recipeJsonAsset;

    [Tooltip("CraftingTable that owns the interact trigger; leave empty to find one in the scene. Burst uses collider center, then a furniture-sized mesh.")]
    [SerializeField] CraftingTable craftingTableSpawn;

    [Tooltip("Added to the spawn point (default 2 up so the effect clears the table mesh).")]
    [SerializeField] Vector3 craftBurstWorldOffset = new Vector3(0f, 2f, 0f);

    [SerializeField]
    [Min(1)]
    int emitCount = 35;

    [Tooltip("Floor for scripted emit size (Hovl presets can be very small in-scene).")]
    [SerializeField]
    float minCraftBurstStartSize = 0.35f;

    RecipeList recipeList;
    CraftingTable _cachedCraftingTable;

    void Start() {
        LoadRecipes();
    }

    CraftingTable ResolveCraftingTable() {
        if (craftingTableSpawn != null)
            return craftingTableSpawn;
        if (_cachedCraftingTable == null)
            _cachedCraftingTable = Object.FindAnyObjectByType<CraftingTable>();
        return _cachedCraftingTable;
    }

    static Vector3 GetCraftBurstWorldPointFromTable(CraftingTable table) {
        if (table == null)
            return Vector3.zero;

        Collider own = table.GetComponent<Collider>();
        if (own != null && own.enabled)
            return own.bounds.center;

        foreach (Collider c in table.GetComponentsInChildren<Collider>(true)) {
            if (c != null && c.enabled && c.isTrigger)
                return c.bounds.center;
        }

        foreach (Collider c in table.GetComponentsInChildren<Collider>(true)) {
            if (c != null && c.enabled)
                return c.bounds.center;
        }

        const float minMeshVol = 0.05f;
        const float maxMeshVol = 480f;
        MeshRenderer[] meshes = table.GetComponentsInChildren<MeshRenderer>(true);
        MeshRenderer best = null;
        float bestVol = 0f;
        for (int i = 0; i < meshes.Length; i++) {
            MeshRenderer mr = meshes[i];
            if (mr == null)
                continue;
            Vector3 s = mr.bounds.size;
            float vol = s.x * s.y * s.z;
            if (vol < minMeshVol || vol > maxMeshVol)
                continue;
            if (vol > bestVol) {
                bestVol = vol;
                best = mr;
            }
        }
        if (best != null)
            return best.bounds.center;

        return table.transform.position;
    }

    void LoadRecipes() {
        if (recipeJsonAsset != null) {
            recipeList = JsonUtility.FromJson<RecipeList>(recipeJsonAsset.text);

            if (recipeList != null && recipeList.recipes != null) {
                if (!RecipesAreValid(recipeList))
                    recipeList = CreateDefaultRecipes();
            } else {
                recipeList = CreateDefaultRecipes();
            }
        } else {
            recipeList = CreateDefaultRecipes();
        }
    }

    static bool RecipesAreValid(RecipeList list) {
        if (list?.recipes == null || list.recipes.Length == 0)
            return false;

        foreach (var recipe in list.recipes) {
            if (recipe.combination == null || recipe.combination.Length != 3)
                return false;
        }

        return true;
    }

    static RecipeList CreateDefaultRecipes() {
        return new RecipeList {
            recipes = new[] {
                new ButterflyRecipe { combination = new[] { 0, 7, 5 }, butterflyID = 0, name = "Ice Monarch" },
                new ButterflyRecipe { combination = new[] { 7, 3, 6 }, butterflyID = 1, name = "Fire Monarch" },
                new ButterflyRecipe { combination = new[] { 4, 3, 2 }, butterflyID = 2, name = "Lightning Monarch" },
                new ButterflyRecipe { combination = new[] { 0, 1, 5 }, butterflyID = 3, name = "Nature Monarch" },
            }
        };
    }

    public Vector3 GetSpawnWorldPosition(int butterflyIndex) {
        Vector3 basePos;

        CraftingTable table = ResolveCraftingTable();
        if (table != null)
            basePos = GetCraftBurstWorldPointFromTable(table);
        else if (butterflyIndex >= 0 && butterflyIndex < butterflySystems.Length &&
                 butterflySystems[butterflyIndex] != null)
            basePos = butterflySystems[butterflyIndex].transform.position;
        else
            basePos = transform.position;

        return basePos + craftBurstWorldOffset;
    }

    public bool TryCraft(int[] currentItems) {
        if (recipeList == null || recipeList.recipes == null)
            return false;

        if (currentItems == null || currentItems.Length != 3)
            return false;

        int[] sorted = (int[])currentItems.Clone();
        System.Array.Sort(sorted);

        foreach (var recipe in recipeList.recipes) {
            if (recipe.combination == null || recipe.combination.Length != 3)
                continue;

            int[] required = (int[])recipe.combination.Clone();
            System.Array.Sort(required);

            if (Enumerable.SequenceEqual(sorted, required)) {
                SpawnSpecificButterfly(recipe.butterflyID);
                return true;
            }
        }

        return false;
    }

    public void SpawnSpecificButterfly(int index) {
        if (butterflySystems == null || butterflySystems.Length == 0)
            return;

        if (index < 0 || index >= butterflySystems.Length)
            return;

        ParticleSystem ps = butterflySystems[index];
        if (ps == null)
            return;

        Vector3 worldPos = GetSpawnWorldPosition(index);

        ps.gameObject.SetActive(true);

        var psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
            psr.enabled = true;

        var emission = ps.emission;
        bool emissionWasOn = emission.enabled;
        emission.enabled = true;

        var main = ps.main;
        var savedSpace = main.simulationSpace;

        Transform tr = ps.transform;
        Vector3 savedPosition = tr.position;
        Quaternion savedRotation = tr.rotation;
        bool repositionedEmitter = false;

        if (Vector3.SqrMagnitude(worldPos - savedPosition) > 0.0001f) {
            tr.SetPositionAndRotation(worldPos, savedRotation);
            repositionedEmitter = true;
        }

        var shape = ps.shape;
        bool shapeWasEnabled = shape.enabled;
        var noise = ps.noise;
        bool noiseWasEnabled = noise.enabled;

        try {
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            shape.enabled = false;
            noise.enabled = false;

            ps.Clear(true);

            if (!ps.isPlaying)
                ps.Play();

            float size = Mathf.Max(main.startSize.constant,
                Mathf.Max(main.startSize.constantMin, main.startSize.constantMax));
            size = Mathf.Max(size, minCraftBurstStartSize);

            const float craftBurstLifetime = 8f;
            var emitParams = new ParticleSystem.EmitParams {
                position = worldPos,
                startSize = size,
                startLifetime = craftBurstLifetime,
                velocity = Vector3.up * 0.35f,
                applyShapeToPosition = false
            };
            ps.Emit(emitParams, emitCount);
        }
        finally {
            shape.enabled = shapeWasEnabled;
            noise.enabled = noiseWasEnabled;
            emission.enabled = emissionWasOn;

            if (repositionedEmitter) {
                tr.SetPositionAndRotation(savedPosition, savedRotation);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            } else {
                main.simulationSpace = savedSpace;
            }
        }
    }
}

[System.Serializable]
public class ButterflyRecipe {
    public int[] combination;
    public int butterflyID;
    public string name;
}

[System.Serializable]
public class RecipeList {
    public ButterflyRecipe[] recipes;
}
