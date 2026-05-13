using System;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace npcAI
{
    public class npcBaseState : IdleState
    {
        protected const float FaceThresholdDegrees = 5f;

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

        protected static bool HasReachedDestination(NavMeshAgent agent) =>
            agent != null
            && !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance
            && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.0001f);

        protected bool RotateTowardPoint(Transform transform, Vector3 worldPoint, float degreesPerSecond)
        {
            var flat = worldPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return true;

            var targetRotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                degreesPerSecond * Time.deltaTime);
            return Quaternion.Angle(transform.rotation, targetRotation) <= FaceThresholdDegrees;
        }

        protected void UpdateTurnThenMove(
            NavMeshAgent agent,
            string locomotionTrigger,
            ref bool preparingMove,
            ref Vector3 moveTarget,
            Action queueNextDestination)
        {
            if (agent == null) return;

            if (preparingMove)
            {
                agent.isStopped = true;
                if (!RotateTowardPoint(npc.transform, moveTarget, npc.TurnSpeed))
                    return;

                agent.isStopped = false;
                SetDestinationOnNavMesh(agent, moveTarget);
                SetStateTrigger(locomotionTrigger);
                preparingMove = false;
                return;
            }

            if (HasReachedDestination(agent))
                queueNextDestination();
        }

        // Random point around center on the agent's NavMesh; minExtraSeparation pushes goals farther for run bursts.
        protected static bool TryPickRandomNavDestinationPoint(
            NavMeshAgent agent,
            Vector3 center,
            float radius,
            float minExtraSeparation,
            out Vector3 destination,
            int maxDiskAttempts = 20,
            int ringFallbackCount = 8)
        {
            destination = default;
            if (agent == null || !agent.isActiveAndEnabled) return false;

            float needDist = Mathf.Max(0.12f, agent.stoppingDistance + 0.2f);
            needDist = Mathf.Min(needDist, radius * 0.45f);
            if (radius < 0.5f) needDist = Mathf.Max(0.05f, radius * 0.28f);
            needDist = Mathf.Max(needDist, minExtraSeparation);

            var minSqr = needDist * needDist;
            var pos = agent.transform.position;

            for (int i = 0; i < maxDiskAttempts; i++)
            {
                var inCircle = UnityEngine.Random.insideUnitCircle * radius;
                var tryPoint = center + new Vector3(inCircle.x, 0f, inCircle.y);
                if (!NavMesh.SamplePosition(tryPoint, out var hit, radius, agent.areaMask)) continue;
                if ((pos - hit.position).sqrMagnitude < minSqr) continue;
                destination = hit.position;
                return true;
            }

            for (int j = 0; j < ringFallbackCount; j++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.0001f) continue;
                var tryPoint = center + new Vector3(dir.x, 0f, dir.y) * (radius * 0.9f);
                if (NavMesh.SamplePosition(tryPoint, out var hit, radius, agent.areaMask)
                    && (pos - hit.position).sqrMagnitude > minSqr * 0.5f)
                {
                    destination = hit.position;
                    return true;
                }
            }

            return false;
        }
    }
}