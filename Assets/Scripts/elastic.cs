using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Vector3 baseScale = new Vector3(1f, 1f, 1f); // 기본 크기
    public float amplitude = 0.5f;  // 최대 변동 폭
    public float frequency = 1f;    // 변화 속도
    public float weightSpeed = 0.5f; // 가중치 증가 속도
    public float power;//초창기 힘
    public float powerWeight;//파워가중량
    public float powerLose;//파워소실량
    public float powerInside;
    public float powerOutside;

    private float weight = 0f; // 현재 가중치

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        

        if (powerLose > 0f)
        {
            
        }

        transform.localScale = new Vector3(1 + powerWeight * 0.05f, 1 - powerWeight * 0.1f, 1 + powerWeight * 0.05f);
    }
    private void OnCollisionEnter(Collision collision)
    {
        powerWeight=power;//파워 가중량 = 파워
        powerLose = 5;


    }
    
}
