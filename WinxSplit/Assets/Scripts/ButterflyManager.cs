using UnityEngine;
using System.Linq;

public class ButterflyManager : MonoBehaviour {
    public ParticleSystem[] butterflySystems;

    public TextAsset recipeJsonAsset;

    [Tooltip("Emit here for every craft type. Leave empty so each ParticleSystem emits at its own transform in the scene.")]
    public Transform spawnPointOverride;

    [SerializeField]
    [Min(1)]
    int emitCount = 35;

    private RecipeList recipeList;

    void Start() {
        LoadRecipes();
    }

    void LoadRecipes() {
        if (recipeJsonAsset != null) {
            recipeList = JsonUtility.FromJson<RecipeList>(recipeJsonAsset.text);

            if (recipeList != null && recipeList.recipes != null) {
                if (!RecipesAreValid(recipeList)) {
                    Debug.LogWarning(
                        "ButterflyManager: Recipe combinations missing from JSON (JsonUtility limitation). Using built-in recipes.");
                    recipeList = CreateDefaultRecipes();
                }
            } else {
                Debug.LogWarning("ButterflyManager: JSON parse failed; using built-in recipes.");
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

    /// <summary>World-space position where this butterfly effect should burst.</summary>
    public Vector3 GetSpawnWorldPosition(int butterflyIndex) {
        if (spawnPointOverride != null)
            return spawnPointOverride.position;

        if (butterflyIndex >= 0 && butterflyIndex < butterflySystems.Length &&
            butterflySystems[butterflyIndex] != null)
            return butterflySystems[butterflyIndex].transform.position;

        return transform.position;
    }

    /// <summary>EmitParams.position must match ParticleSystem simulation space.</summary>
    static Vector3 WorldPositionToEmitSpace(ParticleSystem ps, Vector3 worldPos) {
        var main = ps.main;
        switch (main.simulationSpace) {
            case ParticleSystemSimulationSpace.Local:
                return ps.transform.InverseTransformPoint(worldPos);
            case ParticleSystemSimulationSpace.Custom:
                if (main.customSimulationSpace != null)
                    return main.customSimulationSpace.InverseTransformPoint(worldPos);
                return worldPos;
            default:
                return worldPos;
        }
    }

    /// <summary>Returns true if a recipe matched and particles were emitted.</summary>
    public bool TryCraft(int[] currentItems) {
        if (recipeList == null || recipeList.recipes == null) {
            Debug.LogError("ButterflyManager: No recipes loaded.");
            return false;
        }

        if (currentItems == null || currentItems.Length != 3) {
            Debug.LogWarning("ButterflyManager: Crafting requires exactly 3 items.");
            return false;
        }

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
        if (butterflySystems == null || butterflySystems.Length == 0) {
            Debug.LogError("ButterflyManager: butterflySystems is empty.");
            return;
        }

        if (index < 0 || index >= butterflySystems.Length) {
            Debug.LogError($"ButterflyManager: Invalid butterfly index {index}.");
            return;
        }

        ParticleSystem ps = butterflySystems[index];
        if (ps == null) {
            Debug.LogError($"ButterflyManager: butterflySystems[{index}] is not assigned.");
            return;
        }

        Vector3 worldPos = GetSpawnWorldPosition(index);
        Vector3 emitPos = WorldPositionToEmitSpace(ps, worldPos);

        if (!ps.isPlaying)
            ps.Play();

        var emitParams = new ParticleSystem.EmitParams {
            position = emitPos,
            applyShapeToPosition = false
        };
        ps.Emit(emitParams, emitCount);
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
