using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcWanderState : npcBaseState
    {
        readonly NavMeshAgent agent;
        readonly float wanderRadius;

        public npcWanderState(NPC npc, Animator animator, NavMeshAgent agent, float wanderRadius)
            : base(npc, animator)
        {
            this.agent = agent;
            this.wanderRadius = wanderRadius;
        }

        public override void Enter()
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.speed = npc.WalkSpeed;
            }

            SetStateTrigger("Wander");
            SetRandomDestination();
        }

        public override void Update()
        {
            if (HasReachedDestination())
                SetRandomDestination();
        }

        void SetRandomDestination()
        {
            TryPickRandomNavDestination(agent, npc.TerritoryCenter, wanderRadius, minExtraSeparation: 0f);
        }

        bool HasReachedDestination() =>
            agent != null
            && !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance
            && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.0001f);
    }
}
