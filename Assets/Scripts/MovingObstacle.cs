using UnityEngine;
using System.Collections.Generic;

public class MovingObstacle : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private bool isRepeating = true; // 반복 이동 여부
    [SerializeField] private bool canAutoMove = true; // 자동 이동 여부
    [SerializeField] private float moveSpeed = 5f; // 이동 속도
    [SerializeField] private float waitTime = 0f; // 각 포인트에서 대기 시간

    [Header("이동 포인트")]
    [SerializeField] private Transform[] waypoints; // 이동 경로 포인트들
    [SerializeField] private bool useLocalPositions = false; // 로컬 좌표 사용 여부

    [Header("디버그")]
    [SerializeField] private bool showGizmos = true; // Scene 뷰에서 경로 표시
    [SerializeField] private Color gizmoColor = Color.yellow;

    private int currentWaypointIndex = 0;
    private bool isMovingForward = true; // 앞으로 이동 중인지 (반복 모드에서 사용)
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Vector3 startPosition;
    private bool hasReachedEnd = false; // 한 번만 이동하는 경우 종료 체크
    private bool hasPlayerEnter = false; // 플레이어 탑승 시 활성화

    // 이동 완료 이벤트
    public System.Action OnMovementComplete;

    private void Start()
    {
        // 시작 위치 저장
        startPosition = transform.position;

        // waypoints가 비어있다면 기본 포인트 생성
        if (waypoints == null || waypoints.Length == 0)
        {
            CreateDefaultWaypoints();
        }

        // 첫 번째 포인트가 현재 위치와 다르면 첫 번째 포인트로 이동 시작
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
            hasPlayerEnter = true; // 자동 이동이 아니면 플레이어 감지할 때까지 대기
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
        if (canAutoMove) return; // 자동 이동이 아닐 경우에만 플레이어 감지

        if (other.CompareTag("Player"))
        {
            hasPlayerEnter = false;
        }
    }


    private void MoveTowardsTarget()
    {
        Vector3 targetPosition = GetCurrentTargetPosition();

        // 목표 지점으로 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // 목표 지점에 도달했는지 확인
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
        // 대기 시간이 있다면 대기 시작
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
        // 기본 waypoints 생성 (현재 위치에서 앞으로 10유닛)
        GameObject startPoint = new GameObject("StartPoint");
        GameObject endPoint = new GameObject("EndPoint");

        startPoint.transform.position = transform.position;
        endPoint.transform.position = transform.position + transform.forward * 10f;

        startPoint.transform.SetParent(transform);
        endPoint.transform.SetParent(transform);

        waypoints = new Transform[] { startPoint.transform, endPoint.transform };
    }

    // 런타임에서 속도 변경
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0, newSpeed);
    }

    // 반복 모드 변경
    public void SetRepeating(bool repeating)
    {
        isRepeating = repeating;
        if (!repeating)
        {
            hasReachedEnd = false;
        }
    }

    // 이동 초기화 (처음부터 다시 시작)
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

    // 이동 일시정지/재개
    public void PauseMovement()
    {
        enabled = false;
    }

    public void ResumeMovement()
    {
        enabled = true;
    }

    // waypoint 추가
    public void AddWaypoint(Vector3 position)
    {
        // waypoints 부모 찾기 또는 생성
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

    // Scene 뷰에서 경로 표시
    private void OnDrawGizmos()
    {
        if (!showGizmos || waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = gizmoColor;

        // 경로 선 그리기
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
        // 반복 모드면 마지막에서 첫 번째로 돌아가는 선도 그리기
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

        // waypoint 위치에 구체 그리기
        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Vector3 point = useLocalPositions ?
                    transform.TransformPoint(waypoints[i].localPosition) :
                    waypoints[i].position;

                Gizmos.DrawWireSphere(point, 0.5f);

                // 현재 목표 지점 강조
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