using UnityEngine;
using System.Collections.Generic;

public class WindmillController : MonoBehaviour
{
    [Header("회전 설정")]
    public float rotationSpeed = 50f;

    [Header("날개 설정")]
    [Range(0.5f, 10f)]
    public float bladeDistance = 2f;

    [Header("자동 배치 설정")]
    public bool autoArrangeBlade = true;
    [Range(0f, 360f)]
    public float startAngle = 0f;  // 첫 번째 날개의 시작 각도

    [Header("수동 설정 (autoArrangeBlade가 false일 때만 사용)")]
    public List<Transform> blades = new List<Transform>();

    private Vector3[] originalBladePositions;
    private float currentDistance;
    private bool currentAutoArrange;
    private float currentStartAngle;
    private int previousBladeCount;

    void Start()
    {
        // 자동으로 자식 오브젝트들을 날개로 등록
        if (blades.Count == 0)
        {
            FindBlades();
        }

        InitializeBlade();

        // 현재 상태 저장
        currentDistance = bladeDistance;
        currentAutoArrange = autoArrangeBlade;
        currentStartAngle = startAngle;
        previousBladeCount = blades.Count;
    }

    void Update()
    {
        // 풍차 회전
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // 설정이 변경되었을 때 날개 위치 업데이트
        bool needUpdate = false;

        // 거리 변경 체크
        if (Mathf.Abs(currentDistance - bladeDistance) > 0.01f)
        {
            needUpdate = true;
            currentDistance = bladeDistance;
        }

        // 자동 배치 모드 변경 체크
        if (currentAutoArrange != autoArrangeBlade)
        {
            needUpdate = true;
            currentAutoArrange = autoArrangeBlade;
        }

        // 시작 각도 변경 체크
        if (autoArrangeBlade && Mathf.Abs(currentStartAngle - startAngle) > 0.1f)
        {
            needUpdate = true;
            currentStartAngle = startAngle;
        }

        // 날개 개수 변경 체크
        if (previousBladeCount != blades.Count)
        {
            needUpdate = true;
            previousBladeCount = blades.Count;
        }

        if (needUpdate)
        {
            UpdateBladePositions();
        }
    }

    void FindBlades()
    {
        blades.Clear();

        // 자식 오브젝트 중에서 "Blade" 또는 "Wing"이 포함된 이름의 오브젝트들을 찾음
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("blade") ||
                child.name.ToLower().Contains("wing") ||
                child.name.ToLower().Contains("날개"))
            {
                blades.Add(child);
            }
        }

        // 만약 특별한 이름이 없다면 모든 자식을 날개로 간주
        if (blades.Count == 0)
        {
            foreach (Transform child in transform)
            {
                blades.Add(child);
            }
        }
    }

    void InitializeBlade()
    {
        if (!autoArrangeBlade)
        {
            // 수동 모드일 때는 원래 위치 저장
            SaveOriginalPositions();
        }
        else
        {
            // 자동 배치 모드일 때는 바로 배치
            ArrangeBladesInCircle();
        }
    }

    void SaveOriginalPositions()
    {
        originalBladePositions = new Vector3[blades.Count];
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null)
            {
                originalBladePositions[i] = blades[i].localPosition;
            }
        }
    }

    void ArrangeBladesInCircle()
    {
        if (blades.Count == 0) return;

        float angleStep = 360f / blades.Count;  // 날개 사이의 각도

        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null)
            {
                // 각 날개의 각도 계산 (시작 각도 + 단계별 각도)
                float angle = startAngle + (angleStep * i);
                float radian = angle * Mathf.Deg2Rad;

                // 원형 배치: X-Z 평면에서 배치 (Y축 중심 회전)
                Vector3 position = new Vector3(
                    Mathf.Sin(radian) * bladeDistance,
                    0f,
                    Mathf.Cos(radian) * bladeDistance
                );

                blades[i].localPosition = position;

                // 날개가 중심을 향하도록 회전 (선택사항)
                // blades[i].localRotation = Quaternion.LookRotation(-position.normalized, Vector3.up);
            }
        }

        // 자동 배치 후 현재 위치를 원래 위치로 저장
        SaveOriginalPositions();
    }

    void UpdateBladePositions()
    {
        if (autoArrangeBlade)
        {
            // 자동 배치 모드일 때는 원형으로 재배치
            ArrangeBladesInCircle();
        }
        else
        {
            // 수동 모드일 때는 기존 방식 사용
            UpdateManualBladePositions();
        }
    }

    void UpdateManualBladePositions()
    {
        if (originalBladePositions == null) return;

        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] != null && i < originalBladePositions.Length)
            {
                // 원래 위치에서의 방향을 구함
                Vector3 direction = originalBladePositions[i].normalized;
                // 새로운 거리로 위치 설정
                blades[i].localPosition = direction * bladeDistance;
            }
        }
    }

    // Inspector에서 값 변경 시 실시간 적용
    void OnValidate()
    {
        if (Application.isPlaying && blades != null && blades.Count > 0)
        {
            UpdateBladePositions();
        }
    }

    // 런타임에서 날개를 다시 찾는 함수 (새로운 자식이 추가되었을 때 등)
    [ContextMenu("날개 다시 찾기")]
    public void RefreshBlades()
    {
        FindBlades();
        InitializeBlade();
        UpdateBladePositions();
        previousBladeCount = blades.Count;
    }

    // 현재 배치를 원래 위치로 저장하는 함수
    [ContextMenu("현재 위치 저장")]
    public void SaveCurrentPositions()
    {
        SaveOriginalPositions();
    }
}