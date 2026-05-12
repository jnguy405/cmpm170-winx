using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace npcAI
{
    public class npcBaseState : IdleState
    {
        protected readonly NPC npc;
        protected readonly Animator animator;

        protected npcBaseState(NPC npc, Animator animator)
        {
            this.npc = npc;
            this.animator = animator;
        }

        protected void SetStateTrigger(string triggerName)
        {
            if (animator == null) return;

            ResetAllTriggers(animator);
            animator.SetTrigger(triggerName);
            animator.Update(0f);
        }

        static void ResetAllTriggers(Animator anim)
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger)
                    anim.ResetTrigger(p.name);
            }
        }

        // Project the target onto the navmesh the agent is allowed to use; avoids bad destinations yanking the agent off the mesh.
        protected static bool SetDestinationOnNavMesh(NavMeshAgent nav, Vector3 worldTarget, float sampleRadius = 8f)
        {
            if (nav == null || !nav.isActiveAndEnabled) return false;
            if (NavMesh.SamplePosition(worldTarget, out var hit, sampleRadius, nav.areaMask))
            {
                nav.SetDestination(hit.position);
                return true;
            }
            return false;
        }

        // Random point around center on the agent's NavMesh; minExtraSeparation pushes goals farther for run bursts.
        protected static bool TryPickRandomNavDestination(
            NavMeshAgent agent,
            Vector3 center,
            float radius,
            float minExtraSeparation,
            int maxDiskAttempts = 20,
            int ringFallbackCount = 8)
        {
            if (agent == null || !agent.isActiveAndEnabled) return false;

            float needDist = Mathf.Max(0.12f, agent.stoppingDistance + 0.2f);
            needDist = Mathf.Min(needDist, radius * 0.45f);
            if (radius < 0.5f) needDist = Mathf.Max(0.05f, radius * 0.28f);
            needDist = Mathf.Max(needDist, minExtraSeparation);

            var minSqr = needDist * needDist;
            var pos = agent.transform.position;

            for (int i = 0; i < maxDiskAttempts; i++)
            {
                var inCircle = Random.insideUnitCircle * radius;
                var tryPoint = center + new Vector3(inCircle.x, 0f, inCircle.y);
                if (!NavMesh.SamplePosition(tryPoint, out var hit, radius, agent.areaMask)) continue;
                if ((pos - hit.position).sqrMagnitude < minSqr) continue;
                agent.SetDestination(hit.position);
                return true;
            }

            for (int j = 0; j < ringFallbackCount; j++)
            {
                var dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.0001f) continue;
                var tryPoint = center + new Vector3(dir.x, 0f, dir.y) * (radius * 0.9f);
                if (NavMesh.SamplePosition(tryPoint, out var hit, radius, agent.areaMask)
                    && (pos - hit.position).sqrMagnitude > minSqr * 0.5f)
                {
                    agent.SetDestination(hit.position);
                    return true;
                }
            }

            return false;
        }
    }
}