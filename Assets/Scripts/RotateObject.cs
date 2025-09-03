using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 50f; // ȸ�� �ӵ� (�ʴ� ����)

    void Update()
    {
        // ���� ������Ʈ�� Transform ������Ʈ�� ����       
        // rotationSpeed�� Time.deltaTime�� ���Ͽ� ������ �ӵ��� �������� ȸ���� ����ϴ�.
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}