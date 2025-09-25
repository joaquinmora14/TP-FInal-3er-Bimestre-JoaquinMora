using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class AgentScript : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Patrullaje")]
    [SerializeField] private List<Transform> targets = new List<Transform>();
    private int currentTargetIndex = 0;
    [SerializeField] private float reachThreshold = 0.5f;
    private bool isChasing = false;
    private bool finishedPatrol = false;

    [Header("Animación")]
    [SerializeField] private Animator anim;

    [Header("Detección del jugador")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField, Range(0f, 180f)] private float detectionAngle = 40f;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private float eyeHeight = 1.6f;

    [Header("Captura")]
    [SerializeField] private float catchDistance = 1f;
    private bool gameOverTriggered = false;

    [Header("Persecución")]
    [SerializeField] private float loseSightTime = 2f;
    private float lastSeenTime = Mathf.NegativeInfinity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{name} no tiene NavMeshAgent!");
        agent.updateRotation = true;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (targets != null && targets.Count > 0)
        {
            finishedPatrol = false;
            agent.isStopped = false;
            agent.SetDestination(targets[currentTargetIndex].position);
        }
        else
        {
            finishedPatrol = true;
            agent.isStopped = true;
        }
    }

    private void Update()
    {
        if (gameOverTriggered) return;

        if (!isChasing)
        {
            DetectPlayer();
            Patrol();
        }
        else
        {
            if (player != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);

                if (Time.time - lastSeenTime > loseSightTime)
                {
                    isChasing = false;
                    RestartPatrolFromRandom();
                }
            }
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            GameOver();
            return;
        }

        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void Patrol()
    {
        if (finishedPatrol) return;
        if (targets == null || targets.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= reachThreshold)
        {
            currentTargetIndex++;

            if (currentTargetIndex >= targets.Count)
            {
                currentTargetIndex = 0;
            }

            agent.SetDestination(targets[currentTargetIndex].position);
        }
    }

    private void RestartPatrolFromRandom()
    {
        if (targets == null || targets.Count == 0) return;
        finishedPatrol = false;
        currentTargetIndex = Random.Range(0, targets.Count);
        agent.isStopped = false;
        agent.SetDestination(targets[currentTargetIndex].position);
    }

    private void DetectPlayer()
    {
        if (player == null) return;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - origin;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > detectionAngle) return;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, detectionRange, detectionMask))
        {
            if (hit.transform == player || hit.collider.CompareTag("Player"))
            {
                isChasing = true;
                finishedPatrol = true;
                agent.isStopped = false;
                agent.SetDestination(player.position);
                lastSeenTime = Time.time;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameOverTriggered) return;
        if (other.CompareTag("Player"))
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;
        SceneManager.LoadScene("GameOverScene");
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRange);

        Vector3 forward = transform.forward;
        Quaternion leftRot = Quaternion.Euler(0, -detectionAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, detectionAngle, 0);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, leftDir * detectionRange);
        Gizmos.DrawRay(origin, rightDir * detectionRange);
    }
}