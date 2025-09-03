using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] Collider _collider = null;
    [SerializeField] PlayerController _player;

    private void Start()
    {
        if (_collider == null)
        {
            _collider = GetComponent<Collider>();
        }

        if (_player == null)
        {
            _player = GetComponent<PlayerController>();
        }

        // BoxCollider�� Trigger�� ���� Ȯ��
        if (_collider != null && !_collider.isTrigger)
        {
            Debug.LogWarning("PlayerCollision: Collider should be set as Trigger!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"player trigger enter {other.gameObject.name}");
        _player.IsGrounded = true;
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"player trigger exit {other.gameObject.name}");
        _player.IsGrounded = false;
    }

    private void OnTriggerStay(Collider other)
    {
        

    }
}