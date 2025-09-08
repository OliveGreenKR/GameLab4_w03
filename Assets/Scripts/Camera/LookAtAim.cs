using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class LookAtAim : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Aim Point Manager")]
    [Required]
    [SerializeField] private AimPointManager _aimPointManager;

    [TabGroup("Targets")]
    [Header("Look At Targets")]
    [SerializeField] private List<Transform> _targetTransforms = new List<Transform>();

    [TabGroup("Settings")]
    [Header("Update Settings")]
    [SerializeField] private bool _enableLookAt = true;

    [TabGroup("Settings")]
    [Header("Rotation Settings")]
    [SuffixLabel("units")]
    [PropertyRange(0.001f, 1f)]
    [SerializeField] private float _positionChangeThreshold = 0.1f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsLookAtEnabled => _enableLookAt;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int RegisteredTargetCount => _targetTransforms?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasValidAimManager => _aimPointManager != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentAimPoint => _currentAimPoint;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastAimPoint => _lastAimPoint;
    #endregion

    #region Private Fields
    private Vector3 _currentAimPoint;
    private Vector3 _lastAimPoint;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_aimPointManager == null)
        {
            _aimPointManager = FindFirstObjectByType<AimPointManager>();
            if (_aimPointManager == null)
            {
                Debug.LogError("[LookAtAim] AimPointManager not found in scene!", this);
                _enableLookAt = false;
                return;
            }
        }

        // 초기 AimPoint 설정
        UpdateAimPoint();
        _lastAimPoint = _currentAimPoint;
    }

    private void LateUpdate()
    {
        if (!_enableLookAt || _aimPointManager == null)
            return;

        UpdateAimPoint();

        if (ShouldUpdateRotation())
        {
            UpdateAllTargetRotations();
            _lastAimPoint = _currentAimPoint;
        }
    }
    #endregion

    #region Public Methods - Target Management
    /// <summary>
    /// 타겟 Transform 추가
    /// </summary>
    /// <param name="target">추가할 Transform</param>
    public void AddTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[LookAtAim] Cannot add null target", this);
            return;
        }

        if (_targetTransforms == null)
        {
            _targetTransforms = new List<Transform>();
        }

        if (_targetTransforms.Contains(target))
        {
            Debug.LogWarning($"[LookAtAim] Target {target.name} is already registered", this);
            return;
        }

        _targetTransforms.Add(target);
    }

    /// <summary>
    /// 타겟 Transform 제거
    /// </summary>
    /// <param name="target">제거할 Transform</param>
    /// <returns>제거 성공 여부</returns>
    public bool RemoveTarget(Transform target)
    {
        if (target == null || _targetTransforms == null)
            return false;

        return _targetTransforms.Remove(target);
    }

    /// <summary>
    /// 모든 타겟 제거
    /// </summary>
    public void ClearAllTargets()
    {
        if (_targetTransforms != null)
        {
            _targetTransforms.Clear();
        }
    }

    /// <summary>
    /// LookAt 기능 활성화/비활성화
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetLookAtEnabled(bool enabled)
    {
        _enableLookAt = enabled;
    }
    #endregion

    #region Private Methods - Look At Logic
    private void UpdateAimPoint()
    {
        if (_aimPointManager != null && _aimPointManager.HasValidCamera)
        {
            _currentAimPoint = _aimPointManager.AimPoint;
        }
    }

    private bool ShouldUpdateRotation()
    {
        float distanceChanged = Vector3.Distance(_currentAimPoint, _lastAimPoint);
        return distanceChanged > _positionChangeThreshold;
    }

    private void UpdateAllTargetRotations()
    {
        if (_targetTransforms == null) return;

        for (int i = _targetTransforms.Count - 1; i >= 0; i--)
        {
            if (_targetTransforms[i] == null)
            {
                _targetTransforms.RemoveAt(i);
                continue;
            }

            UpdateTargetRotation(_targetTransforms[i]);
        }
    }

    private void UpdateTargetRotation(Transform target)
    {
        if (target == null) return;

        target.LookAt(_currentAimPoint);
    }
    #endregion
}