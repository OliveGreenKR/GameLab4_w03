using Sirenix.OdinInspector;
using UnityEngine;

public class CircleSegmentSpawner : MonoBehaviour
{
    [SerializeField] private float _radius = 5f; // 반지름
    [SerializeField] private int _segmentCount = 8; // Segments 개수
    [SerializeField] private Vector3 _segmentWorldScale = Vector3.one; // Segments 월드 스케일 (x, y, z)
    [SerializeField] private GameObject _segmentPrefab; // SegmentPrefab


    [Button(ButtonSizes.Large)]
    private void Spawn() => SpawnSegments(_radius, _segmentCount);

    void Start()
    {
        if (_segmentPrefab == null)
        {
            Debug.LogError("SegmentPrefab is not assigned!");
            return;
        }

        if (_segmentCount <= 0)
        {
            Debug.LogError("Segment count must be greater than 0!");
            return;
        }

        SpawnSegments(_radius, _segmentCount);
    }

    private void SpawnSegments(float radius, int segmentsCount)
    {
        float angleStep = 360f / _segmentCount; // 각 세그먼트 사이의 각도

        for (int i = 0; i < segmentsCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad; // 라디안으로 변환
            Vector3 position = transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius); // XZ 평면 기준으로 원형 배치 (Y축은 0으로 가정)
            GameObject segment = Instantiate(_segmentPrefab, position, Quaternion.identity, transform); // 본인의 자손으로 생성
            segment.transform.localScale = _segmentWorldScale; // 월드 스케일로 설정

            segment.transform.LookAt(transform.position); // 중앙을 바라보도록 회전
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; // 원의 색상 설정
        int segments = 36; // 부드러운 원을 위해 더 많은 세그먼트 사용
        float angleStep = 360f / segments;

        Vector3 prevPoint = transform.position + new Vector3(_radius, 0f, 0f); // 시작점 (X축 기준)

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = transform.position + new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius); // XZ 평면 기준
            Gizmos.DrawLine(prevPoint, nextPoint); // 선분 그리기
            prevPoint = nextPoint;
        }
    }
}