using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] Collider _collider = null;
    [SerializeField] PlayerController _player;

    private void Start()
    {
        if(_collider == null )
        {
            _collider = GetComponent<Collider>();
        }

        if(_player == null )
        {
            _player = GetComponent<PlayerController>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"player collision enter {collision.gameObject.name}");
        _player.IsGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"player collision exit {collision.gameObject.name}");
    }

    private void OnCollisionStay(Collision collision)
    {
        //sdfsdfsdfsdfsf
        //sdfsdfsdfs
    }
}
