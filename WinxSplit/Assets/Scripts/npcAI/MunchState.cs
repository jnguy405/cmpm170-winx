using UnityEngine;
using UnityEngine.AI;

namespace npcAI
{
    public class npcMunchState : npcBaseState
    {
        readonly NavMeshAgent agent;
        bool restoreStopped;

        public npcMunchState(NPC npc, Animator animator, NavMeshAgent agent)
            : base(npc, animator)
        {
            this.agent = agent;
        }

        public override void Enter()
        {
            if (agent != null)
            {
                restoreStopped = agent.isStopped;
                agent.isStopped = true;
                agent.ResetPath();
            }

            SetStateTrigger("Munch");
        }

        public override void Exit()
        {
            if (agent != null)
                agent.isStopped = restoreStopped;
        }
    }
}
