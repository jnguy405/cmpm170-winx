using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcRunState : npcBaseState
    {
        readonly NavMeshAgent agent;
        readonly float roamRadius;
        float savedSpeed;
        float savedAngularSpeed;

        public npcRunState(NPC npc, Animator animator, NavMeshAgent agent, float roamRadius)
            : base(npc, animator)
        {
            this.agent = agent;
            this.roamRadius = roamRadius;
        }

        public override void Enter()
        {
            if (agent == null) return;

            savedSpeed = agent.speed;
            savedAngularSpeed = agent.angularSpeed;
            agent.speed = npc.RunSpeed;
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 420f);
            agent.isStopped = false;

            SetStateTrigger("Run");
            TryPickNextRunPoint();
        }

        public override void Update()
        {
            if (HasReachedDestination())
                TryPickNextRunPoint();
        }

        public override void Exit()
        {
            if (agent == null) return;
            agent.speed = savedSpeed;
            agent.angularSpeed = savedAngularSpeed;
        }

        void TryPickNextRunPoint()
        {
            TryPickRandomNavDestination(agent, npc.TerritoryCenter, roamRadius, minExtraSeparation: 1.85f);
        }

        bool HasReachedDestination() =>
            agent != null
            && !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance
            && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.0001f);
    }
}
