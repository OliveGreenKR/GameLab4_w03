using Unity.Cinemachine;
using UnityEngine;

public class AimCamera : MonoBehaviour
{

    [SerializeField] public CinemachineCamera _aimingCamera;
    [SerializeField] public Vector3 _aimedWorldPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _aimedWorldPosition = _aimingCamera.State.ReferenceLookAt;
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
