using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcRunState : npcBaseState
    {
        readonly NavMeshAgent agent;
        readonly float roamRadius;
        float savedSpeed;

        bool preparingMove;
        Vector3 moveTarget;

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
            agent.speed = npc.RunSpeed;
            agent.isStopped = true;

            preparingMove = false;
            QueueNextDestination();
        }

        public override void Update()
        {
            UpdateTurnThenMove(agent, "Run", ref preparingMove, ref moveTarget, QueueNextDestination);
        }

        public override void Exit()
        {
            if (agent == null) return;
            agent.speed = savedSpeed;
        }

        void QueueNextDestination()
        {
            if (TryPickRandomNavDestinationPoint(agent, npc.TerritoryCenter, roamRadius, 1.85f, out moveTarget))
                preparingMove = true;
        }
    }
}
