using UnityEngine;

public class PlayerKnockback : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 knockbackDir;
    private float knockbackTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (knockbackTime > 0)
        {
            controller.Move(knockbackDir * Time.deltaTime);
            knockbackTime -= Time.deltaTime;
        }
    }

    public void Knockback(Vector3 dir, float force, float duration)
    {
        knockbackDir = dir.normalized * force;
        knockbackTime = duration;
    }
}
