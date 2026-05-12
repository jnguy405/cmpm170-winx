using UnityEngine;
using System.IO;
using System.Linq;

public class ButterflyManager : MonoBehaviour {
    public ParticleSystem[] butterflySystems;
    private RecipeList recipeList;

    void Start() {
        LoadRecipes();
    }

    void LoadRecipes() {
        string path = Path.Combine(Application.streamingAssetsPath, "recipes.json");
        if (File.Exists(path)) {
            string jsonString = File.ReadAllText(path);
            recipeList = JsonUtility.FromJson<RecipeList>(jsonString);
        }
    }

    public void CheckRecipe(int[] currentItems, Vector3 spawnPoint) {
        // Sort both arrays so the order they were dropped in doesn't matter
        System.Array.Sort(currentItems);

        foreach (var recipe in recipeList.recipes) {
            int[] required = (int[])recipe.combination.Clone();
            System.Array.Sort(required);

            if (Enumerable.SequenceEqual(currentItems, required)) {
                SpawnSpecificButterfly(recipe.butterflyID);
                return;
            }
        }
        Debug.Log("No recipe found for this combo!");
    }

    public void SpawnSpecificButterfly(int index)
    {
        if (index >= 0 && index < butterflySystems.Length)
        {
            // 1. Create a parameter object
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            
            // 2. Set the exact position (0, 0, 2)
            emitParams.position = new Vector3(0f, 0f, 0f);
            
            // 3. Emit using these parameters
            butterflySystems[index].Emit(emitParams, 1);
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