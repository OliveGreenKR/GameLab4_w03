using UnityEngine;
using System.Collections.Generic;

public class MovingObstacle : MonoBehaviour
{
    [Header("�̵� ����")]
    [SerializeField] private bool isRepeating = true; // �ݺ� �̵� ����
    [SerializeField] private bool canAutoMove = true; // �ڵ� �̵� ����
    [SerializeField] private float moveSpeed = 5f; // �̵� �ӵ�
    [SerializeField] private float waitTime = 0f; // �� ����Ʈ���� ��� �ð�

    [Header("�̵� ����Ʈ")]
    [SerializeField] private Transform[] waypoints; // �̵� ��� ����Ʈ��
    [SerializeField] private bool useLocalPositions = false; // ���� ��ǥ ��� ����

    [Header("�����")]
    [SerializeField] private bool showGizmos = true; // Scene �信�� ��� ǥ��
    [SerializeField] private Color gizmoColor = Color.yellow;

    private int currentWaypointIndex = 0;
    private bool isMovingForward = true; // ������ �̵� ������ (�ݺ� ��忡�� ���)
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector3 startPosition;
    private bool hasReachedEnd = false; // �� ���� �̵��ϴ� ��� ���� üũ
    private bool hasPlayerEnter = false; // �÷��̾� ž�� �� Ȱ��ȭ

    // �̵� �Ϸ� �̺�Ʈ
    public System.Action OnMovementComplete;

    private void Start()
    {
        // ���� ��ġ ����
        startPosition = transform.position;

        // waypoints�� ����ִٸ� �⺻ ����Ʈ ����
        if (waypoints == null || waypoints.Length == 0)
        {
            CreateDefaultWaypoints();
        }

        // ù ��° ����Ʈ�� ���� ��ġ�� �ٸ��� ù ��° ����Ʈ�� �̵� ����
        if (waypoints.Length > 0)
        {
            Vector3 firstPoint = useLocalPositions ?
                transform.TransformPoint(waypoints[0].localPosition) :
                waypoints[0].position;

            if (Vector3.Distance(transform.position, firstPoint) > 0.1f)
            {
                transform.position = firstPoint;
            }
        }

        if (!canAutoMove)
        {
            hasPlayerEnter = true; // �ڵ� �̵��� �ƴϸ� �÷��̾� ������ ������ ���
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length < 2 || hasReachedEnd || hasPlayerEnter)
            return;

        if (isWaiting)
        {
            HandleWait();
        }
        else
        {
            MoveTowardsTarget();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (canAutoMove) return; // �ڵ� �̵��� �ƴ� ��쿡�� �÷��̾� ����

        if (other.CompareTag("Player"))
        {
            hasPlayerEnter = false;
        }
    }


    private void MoveTowardsTarget()
    {
        Vector3 targetPosition = GetCurrentTargetPosition();

        // ��ǥ �������� �̵�
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // ��ǥ ������ �����ߴ��� Ȯ��
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            OnWaypointReached();
        }
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (currentWaypointIndex >= waypoints.Length)
            return transform.position;

        return useLocalPositions ?
            transform.parent.TransformPoint(waypoints[currentWaypointIndex].localPosition) :
            waypoints[currentWaypointIndex].position;
    }

    private void OnWaypointReached()
    {
        // ��� �ð��� �ִٸ� ��� ����
        if (waitTime > 0)
        {
            StartWait();
            return;
        }

        MoveToNextWaypoint();
    }

    private void StartWait()
    {
        isWaiting = true;
        waitTimer = waitTime;
    }

    private void HandleWait()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0)
        {
            isWaiting = false;
            MoveToNextWaypoint();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (isRepeating)
        {
            HandleRepeatingMovement();
        }
        else
        {
            HandleSingleMovement();
        }
    }

    private void HandleRepeatingMovement()
    {
        if (isMovingForward)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = waypoints.Length - 2;
                isMovingForward = false;
            }
        }
        else
        {
            currentWaypointIndex--;
            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 1;
                isMovingForward = true;
            }
        }
    }

    private void HandleSingleMovement()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            hasReachedEnd = true;
            OnMovementComplete?.Invoke();
        }
    }

    private void CreateDefaultWaypoints()
    {
        // �⺻ waypoints ���� (���� ��ġ���� ������ 10����)
        GameObject startPoint = new GameObject("StartPoint");
        GameObject endPoint = new GameObject("EndPoint");

        startPoint.transform.position = transform.position;
        endPoint.transform.position = transform.position + transform.forward * 10f;

        startPoint.transform.SetParent(transform);
        endPoint.transform.SetParent(transform);

        waypoints = new Transform[] { startPoint.transform, endPoint.transform };
    }

    // ��Ÿ�ӿ��� �ӵ� ����
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0, newSpeed);
    }

    // �ݺ� ��� ����
    public void SetRepeating(bool repeating)
    {
        isRepeating = repeating;
        if (!repeating)
        {
            hasReachedEnd = false;
        }
    }

    // �̵� �ʱ�ȭ (ó������ �ٽ� ����)
    public void ResetMovement()
    {
        currentWaypointIndex = 0;
        isMovingForward = true;
        hasReachedEnd = false;
        isWaiting = false;
        waitTimer = 0f;

        if (waypoints.Length > 0)
        {
            transform.position = GetCurrentTargetPosition();
        }
    }

    // �̵� �Ͻ�����/�簳
    public void PauseMovement()
    {
        enabled = false;
    }

    public void ResumeMovement()
    {
        enabled = true;
    }

    // waypoint �߰�
    public void AddWaypoint(Vector3 position)
    {
        // waypoints �θ� ã�� �Ǵ� ����
        GameObject waypointParent = GameObject.Find("MovingObstacle_Waypoints");
        if (waypointParent == null)
        {
            waypointParent = new GameObject("MovingObstacle_Waypoints");
            waypointParent.transform.SetParent(transform.parent);
        }

        GameObject newWaypoint = new GameObject("Waypoint_" + waypoints.Length);
        newWaypoint.transform.position = position;
        newWaypoint.transform.SetParent(waypointParent.transform);

        System.Array.Resize(ref waypoints, waypoints.Length + 1);
        waypoints[waypoints.Length - 1] = newWaypoint.transform;
    }

    // Scene �信�� ��� ǥ��
    private void OnDrawGizmos()
    {
        if (!showGizmos || waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = gizmoColor;

        // ��� �� �׸���
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Vector3 start = useLocalPositions ?
                    transform.TransformPoint(waypoints[i].localPosition) :
                    waypoints[i].position;
                Vector3 end = useLocalPositions ?
                    transform.TransformPoint(waypoints[i + 1].localPosition) :
                    waypoints[i + 1].position;

                Gizmos.DrawLine(start, end);
            }
        }

        /*
        // �ݺ� ���� ���������� ù ��°�� ���ư��� ���� �׸���
        if (isRepeating && waypoints.Length > 2)
        {
            Vector3 lastPoint = useLocalPositions ?
                transform.TransformPoint(waypoints[waypoints.Length - 1].localPosition) :
                waypoints[waypoints.Length - 1].position;
            Vector3 firstPoint = useLocalPositions ?
                transform.TransformPoint(waypoints[0].localPosition) :
                waypoints[0].position;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(lastPoint, firstPoint);
        }

        // waypoint ��ġ�� ��ü �׸���
        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Vector3 point = useLocalPositions ?
                    transform.TransformPoint(waypoints[i].localPosition) :
                    waypoints[i].position;

                Gizmos.DrawWireSphere(point, 0.5f);

                // ���� ��ǥ ���� ����
                if (i == currentWaypointIndex)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(point, 0.3f);
                    Gizmos.color = gizmoColor;
                }
            }
        }
        */
    }
}