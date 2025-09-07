using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterGravity : MonoBehaviour
{
    [SerializeField] private CharacterController _controller = null;
    [SerializeField] private bool _usePhysicsGravityScale = true;

    [ShowIf("@_usePhysicsGravityScale == false")]
    [SerializeField] private float _gravityScale = 9.81f;

    private void Start()
    {
        if (_controller == null)
        {
            _controller = GetComponent<CharacterController>();
        }

        if (_controller == null)
        {
            Debug.LogError("CharacterController component not found on " + gameObject.name);
        }
    }


    private void Update()
    {
        if (!_controller.isGrounded)
        {
            if(_usePhysicsGravityScale)
            {
                Vector3 gravity = Physics.gravity * Time.deltaTime;
                _controller.Move(gravity);
                return;
            }
            else
            {
                Vector3 gravity = Vector3.down * _gravityScale * Time.deltaTime;
                _controller.Move(gravity);
                return;
            }
        }
    }
}
