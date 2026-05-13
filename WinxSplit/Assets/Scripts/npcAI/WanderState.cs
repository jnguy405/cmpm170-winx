using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcWanderState : npcBaseState
    {
        readonly NavMeshAgent agent;
        readonly float wanderRadius;

        bool preparingMove;
        Vector3 moveTarget;

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
                agent.isStopped = true;
                agent.speed = npc.WalkSpeed;
            }

            preparingMove = false;
            QueueNextDestination();
        }

        public override void Update()
        {
            UpdateTurnThenMove(agent, "Walk", ref preparingMove, ref moveTarget, QueueNextDestination);
        }

        void QueueNextDestination()
        {
            if (TryPickRandomNavDestinationPoint(agent, npc.TerritoryCenter, wanderRadius, 0f, out moveTarget))
                preparingMove = true;
        }
    }
}
