using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace npcAI
{
    // Assign animal prefab roots. Use the registry inspector / tooling to stamp <see cref="NPCModelSetup"/>.
    [DisallowMultipleComponent]
    public class NPCModelRegistry : MonoBehaviour
    {
        [Header("NPC prefab roots")]
        [Tooltip("One entry per character prefab root. Apply NPCModelSetup to add NavMeshAgent + NPC.")]
        [FormerlySerializedAs("enemyModelPrefabs")]
        [SerializeField] GameObject[] npcModelPrefabs = Array.Empty<GameObject>();

        public GameObject[] GetAllModelPrefabs() => npcModelPrefabs;

        public GameObject GetRandomSpawnPrefab(System.Random rng)
        {
            if (npcModelPrefabs == null || npcModelPrefabs.Length == 0) return null;

            int c = 0;
            for (int i = 0; i < npcModelPrefabs.Length; i++)
            {
                if (npcModelPrefabs[i] != null) c++;
            }
            if (c == 0) return null;

            int pick = rng.Next(0, c);
            for (int i = 0; i < npcModelPrefabs.Length; i++)
            {
                if (npcModelPrefabs[i] == null) continue;
                if (pick == 0) return npcModelPrefabs[i];
                pick--;
            }
            return null;
        }
    }
}
