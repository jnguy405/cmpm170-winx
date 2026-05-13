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

        protected static bool HasReachedDestination(NavMeshAgent agent)
        {
            if (agent == null || !agent.isActiveAndEnabled || agent.isStopped)
                return false;

            if (!agent.hasPath || agent.pathPending)
                return false;

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                return false;

            if (agent.remainingDistance > agent.stoppingDistance)
                return false;

            return agent.velocity.sqrMagnitude < 0.01f;
        }

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
            string locomotionStateName,
            ref bool preparingMove,
            ref bool awaitingLocomotionStart,
            ref Vector3 moveTarget,
            Action queueNextDestination)
        {
            if (agent == null) return;

            if (preparingMove)
            {
                agent.isStopped = true;
                if (!RotateTowardPoint(agent.transform, moveTarget, npc.TurnSpeed))
                    return;

                SetStateTrigger(locomotionStateName);
                preparingMove = false;
                awaitingLocomotionStart = npc.WaitForLocomotionStart;
                if (!awaitingLocomotionStart)
                    BeginLocomotion(agent, moveTarget, ref awaitingLocomotionStart, queueNextDestination);
                return;
            }

            if (awaitingLocomotionStart)
            {
                agent.isStopped = true;
                if (!ReadyToStartLocomotion(locomotionStateName))
                    return;

                BeginLocomotion(agent, moveTarget, ref awaitingLocomotionStart, queueNextDestination);
                return;
            }

            if (!agent.hasPath)
            {
                if (!agent.pathPending)
                    queueNextDestination();
                return;
            }

            if (HasReachedDestination(agent))
                queueNextDestination();

            if (agent.hasPath)
            {
                Vector3 facePoint = agent.velocity.sqrMagnitude > 0.01f
                    ? agent.transform.position + agent.velocity
                    : agent.steeringTarget;
                RotateTowardPoint(agent.transform, facePoint, npc.TurnSpeed);
            }
        }

        void BeginLocomotion(
            NavMeshAgent agent,
            Vector3 moveTarget,
            ref bool awaitingLocomotionStart,
            Action queueNextDestination)
        {
            agent.isStopped = false;
            if (!SetDestinationOnNavMesh(agent, moveTarget))
            {
                awaitingLocomotionStart = false;
                queueNextDestination();
                return;
            }

            awaitingLocomotionStart = false;
        }

        protected bool ReadyToStartLocomotion(string locomotionStateName)
        {
            if (animator == null || !npc.WaitForLocomotionStart)
                return true;

            int locomotionHash = Animator.StringToHash(locomotionStateName);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == locomotionHash)
                return true;

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == locomotionHash)
                    return true;
            }

            if (IsIdleBlendState(current))
            {
                float loopTime = current.normalizedTime % 1f;
                return loopTime >= npc.LocomotionStartNormalizedTime;
            }

            return true;
        }

        static bool IsIdleBlendState(AnimatorStateInfo state) =>
            state.IsName("Blend Tree") || state.IsName("BlendTree");

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