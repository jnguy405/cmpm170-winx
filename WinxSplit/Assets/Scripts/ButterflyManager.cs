using UnityEngine;
using System.Linq;

public class ButterflyManager : MonoBehaviour {
    public ParticleSystem[] butterflySystems;
    
    public TextAsset recipeJsonAsset; 
    
    private RecipeList recipeList;

    void Start() {
        LoadRecipes();
    }

    void LoadRecipes() {
        if (recipeJsonAsset != null) {
            recipeList = JsonUtility.FromJson<RecipeList>(recipeJsonAsset.text);
            
            if (recipeList != null && recipeList.recipes != null) {
                Debug.Log($"Successfully loaded {recipeList.recipes.Length} recipes from TextAsset.");
            } else {
                Debug.LogError("JSON found but failed to parse. Check your JSON structure!");
            }
        } else {
            Debug.LogError("No JSON asset assigned to ButterflyManager in the Inspector!");
        }
    }

    public void CheckRecipe(int[] currentItems, Vector3 spawnPoint) {
        if (recipeList == null || recipeList.recipes == null) return;

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

    public void SpawnSpecificButterfly(int index) {
        if (index >= 0 && index < butterflySystems.Length) {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = new Vector3(30.18f, 3f, 49.37f);
            butterflySystems[index].Emit(emitParams, 10);
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