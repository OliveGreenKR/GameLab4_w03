using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Vector3 baseScale = new Vector3(1f, 1f, 1f); // �⺻ ũ��
    public float amplitude = 0.5f;  // �ִ� ���� ��
    public float frequency = 1f;    // ��ȭ �ӵ�
    public float weightSpeed = 0.5f; // ����ġ ���� �ӵ�
    public float power;//��â�� ��
    public float powerWeight;//�Ŀ����߷�
    public float powerLose;//�Ŀ��ҽǷ�
    public float powerInside;
    public float powerOutside;

    private float weight = 0f; // ���� ����ġ

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
        powerWeight=power;//�Ŀ� ���߷� = �Ŀ�
        powerLose = 5;


    }
    
}
