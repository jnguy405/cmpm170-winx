using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace npcAI
{
    // Simple animal-style FSM: randomly cycles wander / run using time ranges (no player sensing).
    // Expects animator triggers named Wander and Run if an animator is assigned.
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPC : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float wanderRadius = 10f;
        [SerializeField] float runRoamRadius = 14f;
        [SerializeField] float walkSpeed = 1.75f;
        [SerializeField] float runSpeed = 4.5f;

        [Header("How long each mood lasts (seconds, random in range)")]
        [SerializeField] Vector2 wanderDuration = new Vector2(8f, 20f);
        [SerializeField] Vector2 runDuration = new Vector2(3f, 8f);

        [Header("After minimum stay, small chance each frame to switch (keeps motion from feeling metronomic)")]
        [SerializeField] float earlySwitchChancePerSecond = 0.08f;

        NavMeshAgent agent;
        Vector3 territoryCenter;

        npcWanderState wanderState;
        npcRunState runState;

        IdleState current;
        float stateEndsAt;
        float minStateEndsAt;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public Vector3 TerritoryCenter => territoryCenter;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            territoryCenter = transform.position;
            wanderState = new npcWanderState(this, animator, agent, wanderRadius);
            runState = new npcRunState(this, animator, agent, runRoamRadius);
        }

        void Start()
        {
            territoryCenter = transform.position;
            EnterState(wanderState, wanderDuration);
        }

        void Update()
        {
            current?.Update();

            if (Time.time >= stateEndsAt)
            {
                PickRandomNextState();
                return;
            }

            if (Time.time >= minStateEndsAt && earlySwitchChancePerSecond > 0f)
            {
                if (Random.value < earlySwitchChancePerSecond * Time.deltaTime)
                    PickRandomNextState();
            }
        }

        void EnterState(IdleState next, Vector2 durationRange)
        {
            current?.Exit();
            current = next;
            current?.Enter();

            var span = Mathf.Max(0.05f, Random.Range(durationRange.x, durationRange.y));
            stateEndsAt = Time.time + span;
            minStateEndsAt = Time.time + span * Random.Range(0.35f, 0.65f);
        }

        void PickRandomNextState()
        {
            IdleState next;
            do
            {
                int roll = Random.Range(0, 2);
                next = roll == 0 ? wanderState : runState;
            } while (next == current);

            if (next == wanderState)
                EnterState(wanderState, wanderDuration);
            else
                EnterState(runState, runDuration);
        }
    }
}
