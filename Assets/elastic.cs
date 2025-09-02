using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Vector3 baseScale = new Vector3(1f, 1f, 1f); // �⺻ ũ��
    public float amplitude = 0.5f;  // �ִ� ���� ��
    public float frequency = 1f;    // ��ȭ �ӵ�
    public float weightSpeed = 0.5f; // ����ġ ���� �ӵ�

    private float weight = 0f; // ���� ����ġ

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // �ð��� �����鼭 ����ġ ���� �� ���� �������� ��ȭ�Ǵ� ����
        weight = Mathf.Sin(Time.time * weightSpeed);

        // sin ��� ����ġ ���ϱ�
        float scaleFactor = 1f + Mathf.Sin(Time.time * frequency) * amplitude * weight;

        // X, Y, Z�࿡ ���ÿ� ����
        transform.localScale = baseScale * scaleFactor;
    }
}
