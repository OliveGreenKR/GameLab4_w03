using Sirenix.OdinInspector;
using UnityEngine;
public class AimPointManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Raycast Settings")]
    [PropertyRange(1f, 1000f)]
    [SuffixLabel("units")]
    [SerializeField] private float _rayDistanceUnits = 100f;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask _layerMask = -1;

    [Header("Camera Reference")]
    [SerializeField] private Camera _aimCamera = null;
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public Vector3 AimPoint { get; private set; } = Vector3.zero;

    [ShowInInspector, ReadOnly]
    public bool HasValidCamera => _aimCamera != null;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (_aimCamera == null)
        {
            _aimCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        _aimCamera = null;
    }

    private void LateUpdate()
    {
        UpdateAimPoint();
    }

    private void OnDrawGizmos()
    {
        if (HasValidCamera)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(AimPoint, 0.1f);

            // 레이 표시
            Gizmos.color = Color.yellow;
            Vector3 rayOrigin = _aimCamera.transform.position;
            Vector3 rayEnd = rayOrigin + _aimCamera.transform.forward * _rayDistanceUnits;
            Gizmos.DrawLine(rayOrigin, rayEnd);
        }
    }
    #endregion

    #region Private Methods - Aim Calculation
    private void UpdateAimPoint()
    {
        if (_aimCamera == null) return;

        Vector3 rayOrigin = _aimCamera.transform.position;
        Vector3 rayDirection = _aimCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, _rayDistanceUnits, _layerMask))
        {
            AimPoint = hit.point;
        }
        else
        {
            AimPoint = rayOrigin + rayDirection * _rayDistanceUnits;
        }
    }
    #endregion
}