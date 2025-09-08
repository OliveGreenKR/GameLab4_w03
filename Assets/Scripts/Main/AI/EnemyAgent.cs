using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAgent : MonoBehaviour
{
    [SerializeField] Transform _targetTransform = null;
    [SerializeField][Required] NavMeshAgent _agent = null;

    void Start()
    {
        // Disable automatic traversal so we can handle the jump with our code.
        _agent.autoTraverseOffMeshLink = false;
    }

    void Update()
    {
        // Check if the agent is on an OffMeshLink.
        if (_agent.isOnOffMeshLink)
        {
            Debug.Log("Jumping OffMeshLink");
            StartCoroutine(JumpOffMeshLink());
        }
        else
        {
            // If not jumping, set the destination to the target.
            if (_targetTransform != null)
            {
                _agent.SetDestination(_targetTransform.position);
            }
        }
    }

    // Coroutine to handle the jump motion.
    private IEnumerator JumpOffMeshLink()
    {
        // Get the data for the current OffMeshLink.
        OffMeshLinkData linkData = _agent.currentOffMeshLinkData;

        // Get the start and end positions of the link.
        Vector3 startPos = transform.position;
        Vector3 endPos = linkData.endPos;

        float jumpDuration = 1.0f; // The total time the jump will take.
        float timer = 0f;

        // Loop for the duration of the jump.
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / jumpDuration;

            // Use Vector3.Lerp for horizontal movement and a sine wave for the arc.
            Vector3 jumpPos = Vector3.Lerp(startPos, endPos, t);
            jumpPos.y += Mathf.Sin(t * Mathf.PI) * _agent.height; // Adjust jump height based on agent's height.

            _agent.transform.position = jumpPos;
            yield return null;
        }

        // Once the jump is complete, tell the agent to resume its path.
        _agent.CompleteOffMeshLink();
    }
}