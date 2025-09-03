using UnityEngine;
using System.Collections.Generic;

public class WindmillController : MonoBehaviour
{
    [Header("회전 설정")]
    public float rotationSpeed = 50f;

    [Header("날개 설정")]
    [Range(0.5f, 10f)]
    public float bladeDistance = 2f;
    public List<Transform> blades = new List<Transform>();

    private Vector3[] originalBladePositions;
    private float currentDistance;

    void Start()
    {
        // 자동으로 자식 오브젝트들을 날개로 등록
        if (blades.Count == 0)
        {
            FindBlades();
        }

        // 원래 위치 저장
        SaveOriginalPositions();
        currentDistance = bladeDistance;
    }

    void Update()
    {
        // 풍차 회전
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // 거리가 변경되었을 때 날개 위치 업데이트
        if (Mathf.Abs(currentDistance - bladeDistance) > 0.01f)
        {
            UpdateBladePositions();
            currentDistance = bladeDistance;
        }
    }

    void FindBlades()
    {
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

    void UpdateBladePositions()
    {
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
        if (Application.isPlaying && originalBladePositions != null)
        {
            UpdateBladePositions();
        }
    }

    // 디버그용 UI 정보 표시
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"날개 거리: {bladeDistance:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"회전 속도: {rotationSpeed:F1}");
    }
}