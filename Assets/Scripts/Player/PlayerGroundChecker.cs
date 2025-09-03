using UnityEngine;

public class PlayerGroundChecker : MonoBehaviour
{
    [SerializeField] Collider _collider = null;
    [SerializeField] PlayerController _player;
    [SerializeField] bool IsLog = false;


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

        // BoxCollider를 Trigger로 설정 확인
        if (_collider != null && !_collider.isTrigger)
        {
            Debug.LogWarning("PlayerCollision: Collider should be set as Trigger!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(IsLog)
        {
            Debug.Log($"player trigger enter {other.gameObject.name}");
        }
        _player.IsGrounded = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if(IsLog)
        {
            Debug.Log($"player trigger exit {other.gameObject.name}");
        }
        _player.IsGrounded = false;
    }

    private void OnTriggerStay(Collider other)
    {
        

    }
}