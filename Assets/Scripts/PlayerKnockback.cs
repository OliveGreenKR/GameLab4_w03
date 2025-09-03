using UnityEngine;

public class PlayerKnockback : MonoBehaviour
{
    public float knockbackForce = 20f; // �з����� ��

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // �浹 ������ �÷��̾� ���� ����
                Vector3 dir = (collision.transform.position - transform.position).normalized;

                // ���������� ���� �� ���ϱ� (�˹�)
                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }
        }
    }
}

