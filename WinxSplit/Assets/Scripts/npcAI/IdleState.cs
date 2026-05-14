using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcIdleState : npcBaseState
    {
        readonly NavMeshAgent agent;

        public npcIdleState(NPC npc, Animator animator, NavMeshAgent agent)
            : base(npc, animator)
        {
            this.agent = agent;
        }

        public override void Enter()
        {
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            SetStateTrigger("Idle");
        }
    }
}
