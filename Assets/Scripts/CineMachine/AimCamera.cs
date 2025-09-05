using Unity.Cinemachine;
using UnityEngine;

public class AimCamera : MonoBehaviour
{

    [SerializeField] private CinemachineCamera _aimingCamera;
    [SerializeField] private Transform _aimTransform;
    [SerializeField] private Vector3 _aimedWorldPosition;

    public Vector3 AimedWorldPosition => _aimedWorldPosition;
    public Transform AimTransform => _aimTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _aimedWorldPosition = _aimingCamera.State.ReferenceLookAt;
        _aimTransform.position = _aimedWorldPosition;
    }

    // Scene View에 디버깅용 구체 그리기
    void OnDrawGizmos()
    {
        if (_aimedWorldPosition != Vector3.zero)
        {
            // 조준 위치에 빨간색 구체 그리기
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_aimedWorldPosition, 0.3f); // 반지름 0.2f인 구체
        }
    }
}
