using UnityEngine;
using UnityEngine.AI;
using ithappy.Animals_FREE;

namespace npcAI
{
    // Stamps NavMeshAgent + <see cref="NPC"/> on a prefab root. Used at runtime and by editor tooling.
    public static class NPCModelSetup
    {
        public static void ApplyToRoot(GameObject root)
        {
            if (root == null) return;

            DisablePlayerLocomotion(root);

            if (root.GetComponent<NavMeshAgent>() == null)
                root.AddComponent<NavMeshAgent>();

            var agent = root.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.updateRotation = false;
                agent.updatePosition = true;
            }

            if (root.GetComponent<NPC>() == null)
                root.AddComponent<NPC>();
        }

        static void DisablePlayerLocomotion(GameObject root)
        {
            foreach (var input in root.GetComponentsInChildren<MovePlayerInput>(true))
                input.enabled = false;

            foreach (var mover in root.GetComponentsInChildren<CreatureMover>(true))
                mover.enabled = false;
        }
    }
}
