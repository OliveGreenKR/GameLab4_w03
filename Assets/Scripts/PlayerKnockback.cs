using UnityEngine;

public class PlayerKnockback : MonoBehaviour
{
    public float knockbackForce = 20f; // 밀려나는 힘

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 충돌 지점과 플레이어 사이 방향
                Vector3 dir = (collision.transform.position - transform.position).normalized;

                // 순간적으로 강한 힘 가하기 (넉백)
                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }
        }
    }
}

