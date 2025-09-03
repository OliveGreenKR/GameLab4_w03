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
                    // �浹 ������ �� �߽ɺ��� �������� Ȯ��
                    if (contact.point.y > transform.position.y + 0.1f)
                    {
                        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            // ���� �������� ���� ���� ������Ű��
                            rb.AddForce(Vector3.up * jumpForce, ForceMode.Force);
                        }
                    }
                }
            }
        }
    }
}
