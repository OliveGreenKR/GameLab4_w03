using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Vector3 baseScale = new Vector3(1f, 1f, 1f); // 기본 크기
    public float amplitude = 0.5f;  // 최대 변동 폭
    public float frequency = 1f;    // 변화 속도
    public float weightSpeed = 0.5f; // 가중치 증가 속도

    private float weight = 0f; // 현재 가중치

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 시간이 지나면서 가중치 증가 → 점점 강해졌다 완화되는 느낌
        weight = Mathf.Sin(Time.time * weightSpeed);

        // sin 곡선에 가중치 곱하기
        float scaleFactor = 1f + Mathf.Sin(Time.time * frequency) * amplitude * weight;

        // X, Y, Z축에 동시에 적용
        transform.localScale = baseScale * scaleFactor;
    }
}
