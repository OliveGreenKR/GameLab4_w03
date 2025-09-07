using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform _lookAtTransform;
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private AimCamera _aimCamera;


    private void Update()
    {
        if (_aimCamera != null)
        {
            _lookAtTransform = _aimCamera.AimTransform;
        }

        if (_camera != null && _lookAtTransform != null)
        {
            _camera.Target.LookAtTarget  = _lookAtTransform;
            Debug.Log($"LookAtCamera Update LookAtTarget : {_camera.Target.LookAtTarget.name}" );
        }
    }
}
