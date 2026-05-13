using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace npcAI
{
    // Simple animal-style FSM: randomly cycles idle / walk / run using time ranges (no player sensing).
    // Expects animator triggers named Idle, Walk, and Run if an animator is assigned.
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPC : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] float wanderRadius = 10f;
        [SerializeField] float runRoamRadius = 14f;
        [SerializeField] float walkSpeed = 1.75f;
        [SerializeField] float runSpeed = 4.5f;
        [SerializeField] float turnSpeed = 360f;

        [Header("Locomotion animation gate")]
        [SerializeField] bool waitForLocomotionStart = true;
        [SerializeField, Range(0f, 1f)] float locomotionStartNormalizedTime = 0.9f;

        [Header("How long each mood lasts (seconds, random in range)")]
        [SerializeField] Vector2 idleDuration = new Vector2(3f, 10f);
        [SerializeField] Vector2 wanderDuration = new Vector2(8f, 20f);
        [SerializeField] Vector2 runDuration = new Vector2(3f, 8f);

        [Header("After minimum stay, small chance each frame to switch (keeps motion from feeling metronomic)")]
        [SerializeField] float earlySwitchChancePerSecond = 0.08f;

        NavMeshAgent agent;
        Vector3 territoryCenter;

        npcIdleState idleState;
        npcWanderState wanderState;
        npcRunState runState;

        IdleState current;
        float stateEndsAt;
        float minStateEndsAt;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float TurnSpeed => turnSpeed;
        public bool WaitForLocomotionStart => waitForLocomotionStart;
        public float LocomotionStartNormalizedTime => locomotionStartNormalizedTime;
        public Vector3 TerritoryCenter => territoryCenter;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = GetComponentInChildren<NavMeshAgent>(true);

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            territoryCenter = transform.position;
            idleState = new npcIdleState(this, animator, agent);
            wanderState = new npcWanderState(this, animator, agent, wanderRadius);
            runState = new npcRunState(this, animator, agent, runRoamRadius);
        }

        void Start()
        {
            territoryCenter = transform.position;
            EnterState(idleState, idleDuration);
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
                int roll = Random.Range(0, 3);
                next = roll switch
                {
                    0 => idleState,
                    1 => wanderState,
                    _ => runState
                };
            } while (next == current);

            if (next == idleState)
                EnterState(idleState, idleDuration);
            else if (next == wanderState)
                EnterState(wanderState, wanderDuration);
            else
                EnterState(runState, runDuration);
        }
    }
}
