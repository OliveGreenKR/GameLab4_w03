using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    void Update()
    {
        // ť���� ���� ȸ���� �׻� (0, 0, 0)���� �缳���մϴ�.
        // �̷� ���� ť�갡 �θ� ������Ʈ�� ȸ�� ������ ���� �ʰ�
        // �׻� ���� ����(���� �� ����)�� �ٶ󺸰� �˴ϴ�.
        transform.rotation = Quaternion.identity;
    }
}