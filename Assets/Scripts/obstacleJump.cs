using UnityEngine;

public class obstacleJump : MonoBehaviour
{
    public float jumpForce;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    // 충돌 지점이 내 중심보다 위쪽인지 확인
                    if (contact.point.y > transform.position.y + 0.1f)
                    {
                        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            // 위쪽 방향으로 힘을 가해 점프시키기
                            rb.AddForce(Vector3.up * jumpForce, ForceMode.Force);
                        }
                    }
                }
            }
        }
    }
}
