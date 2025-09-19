using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 터렛 부채꼴 탐지 컴포넌트 (폴링 기반)
/// TurretSectorSettings 참조하여 적 탐지 실행
/// </summary>
public class TurretSectorDetection : MonoBehaviour
{
    private const float KINDA_SMALL = 0.001f;

    #region Serialized Fields
    [TabGroup("References")]
    [Header("Settings Reference")]
    [Required]
    [SerializeField] private TurretSectorSettings _sectorSettings;

    [TabGroup("Detection")]
    [Header("Trigger Collider")]
    [Required]
    [InfoBox("탐지용 트리거 콜라이더 ")]
    [SerializeField] private SphereCollider _triggerCollider;

    [TabGroup("Detection")]
    [Header("Collider Auto-Sizing")]
    [InfoBox("탐지 반지름 대비 콜라이더 크기 배수")]
    [SuffixLabel("multiplier")]
    [PropertyRange(1.0f, 2.0f)]
    [SerializeField] private float _colliderSizeMultiplier = 1.2f;

    [TabGroup("Performance")]
    [Header("Update Settings")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _filterUpdateInterval = 0.1f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int RegisteredEnemyCount => _registeredEnemies.Count;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int DetectedEnemyCount => _detectedEnemies.Count;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float NextFilterTime { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 DetectionCenter => transform.position;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 DetectionForward => transform.forward;
    #endregion

    #region Private Fields
    private HashSet<IBattleEntity> _registeredEnemies = new HashSet<IBattleEntity>();
    private List<IBattleEntity> _detectedEnemies = new List<IBattleEntity>();
    private List<IBattleEntity> _tempDetectedList = new List<IBattleEntity>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponent();
    }

    private void Start()
    {
        ValidateReferences();
        SubscribeToSettings();
        NextFilterTime = Time.time + _filterUpdateInterval;
        IsInitialized = true;
    }

    private void Update()
    {
        if (!IsInitialized) return;

        if (Time.time >= NextFilterTime)
        {
            UpdateDetectionFilter();
            NextFilterTime = Time.time + _filterUpdateInterval;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IBattleEntity enemy = other.GetComponent<IBattleEntity>();
        if (enemy != null && enemy.IsAlive)
        {
            RegisterEnemy(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IBattleEntity enemy = other.GetComponent<IBattleEntity>();
        if (enemy != null)
        {
            UnregisterEnemy(enemy);
        }
    }

    private void OnValidate()
    {
        _filterUpdateInterval = Mathf.Clamp(_filterUpdateInterval, 0.05f, 0.2f);
    }

    private void OnDestroy()
    {
        UnsubscribeFromSettings();
    }
    #endregion

    #region Private Methods - Collider Management
    private void UpdateColliderSize()
    {
        if (_triggerCollider == null || _sectorSettings == null)
            return;

        SphereCollider sphereCollider = _triggerCollider as SphereCollider;
        if (sphereCollider == null)
        {
            Debug.LogWarning("[TurretSectorDetection] Trigger collider is not a SphereCollider. Auto-sizing skipped.", this);
            return;
        }

        float targetRadius = CalculateColliderRadius();
        sphereCollider.radius = targetRadius;

        Debug.Log($"[TurretSectorDetection] Collider radius updated to {targetRadius:F1} units", this);
    }

    private float CalculateColliderRadius()
    {
        if (_sectorSettings == null)
            return 1f;

        float baseRadius = _sectorSettings.DetectionRadius;
        float scaledRadius = baseRadius * _colliderSizeMultiplier;

        // 안전한 범위로 제한
        return Mathf.Clamp(scaledRadius, 1f, 100f);
    }

    private bool ValidateColliderForAutoSizing()
    {
        if (_triggerCollider == null)
            return false;

        if (!_triggerCollider.isTrigger)
        {
            Debug.LogWarning("[TurretSectorDetection] Collider should be set as trigger for auto-sizing", this);
            return false;
        }

        SphereCollider sphereCollider = _triggerCollider as SphereCollider;
        if (sphereCollider == null)
        {
            Debug.LogWarning("[TurretSectorDetection] Only SphereCollider is supported for auto-sizing", this);
            return false;
        }

        return true;
    }
    #endregion

    #region Public Methods - Detection Query
    /// <summary>현재 탐지된 적 목록 (읽기 전용)</summary>
    /// <returns>탐지된 적 리스트</returns>
    public IReadOnlyList<IBattleEntity> GetDetectedEnemies()
    {
        return _detectedEnemies;
    }

    /// <summary>탐지된 적 개수</summary>
    /// <returns>적 개수</returns>
    public int GetDetectedEnemyCount()
    {
        return _detectedEnemies.Count;
    }

    /// <summary>특정 적이 탐지되었는지 확인</summary>
    /// <param name="enemy">확인할 적</param>
    /// <returns>탐지 여부</returns>
    public bool IsEnemyDetected(IBattleEntity enemy)
    {
        return enemy != null && _detectedEnemies.Contains(enemy);
    }

    /// <summary>가장 가까운 탐지된 적</summary>
    /// <returns>가장 가까운 적, 없으면 null</returns>
    public IBattleEntity GetClosestDetectedEnemy()
    {
        if (_detectedEnemies.Count == 0) return null;

        IBattleEntity closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector3 center = DetectionCenter;

        foreach (var enemy in _detectedEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            Vector3 toEnemy = enemy.Transform.position - center;
            float sqrDistance = toEnemy.sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = enemy;
            }
        }

        return closest;
    }
    #endregion

    #region Public Methods - Settings Update
    /// <summary>설정 변경 시 탐지 영역 업데이트</summary>
    public void RefreshDetectionArea()
    {
        if (!IsInitialized) return;

        // 즉시 필터링 업데이트
        ForceFilterUpdate();

        Debug.Log("[TurretSectorDetection] Detection area refreshed", this);
    }

    /// <summary>즉시 필터링 업데이트 실행</summary>
    public void ForceFilterUpdate()
    {
        UpdateDetectionFilter();
        NextFilterTime = Time.time + _filterUpdateInterval;
    }
    #endregion

    #region Private Methods - Detection Logic
    private void UpdateDetectionFilter()
    {
        if (_sectorSettings == null) return;

        ClearInvalidEnemies();
        FilterRegisteredEnemies();
    }

    private void FilterRegisteredEnemies()
    {
        _tempDetectedList.Clear();

        foreach (var enemy in _registeredEnemies)
        {
            if (IsEnemyInSector(enemy))
            {
                _tempDetectedList.Add(enemy);
            }
        }

        _detectedEnemies.Clear();
        _detectedEnemies.AddRange(_tempDetectedList);
    }

    private bool IsEnemyInSector(IBattleEntity enemy)
    {
        if (enemy == null || !enemy.IsAlive) return false;

        return IsEnemyInRange(enemy) && IsEnemyInAngle(enemy);
    }

    private bool IsEnemyInRange(IBattleEntity enemy)
    {
        Vector3 toEnemy = enemy.Transform.position - DetectionCenter;
        toEnemy.y = 0f; // Y축 무시

        float sqrDistance = toEnemy.sqrMagnitude;
        float detectionRadius = _sectorSettings.DetectionRadius;

        return sqrDistance <= (detectionRadius * detectionRadius);
    }

    private bool IsEnemyInAngle(IBattleEntity enemy)
    {
        Vector3 toEnemy = enemy.Transform.position - DetectionCenter;
        toEnemy.y = 0f; // Y축 무시

        if (toEnemy.sqrMagnitude < KINDA_SMALL) return true;

        Vector3 forward = DetectionForward;
        forward.y = 0f;

        if (forward.sqrMagnitude < KINDA_SMALL) return false;

        // 내적으로 각도 계산 (성능 최적화)
        float dotProduct = Vector3.Dot(forward.normalized, toEnemy.normalized);
        float halfSectorAngle = _sectorSettings.SectorAngleDegrees * 0.5f;
        float cosHalfAngle = Mathf.Cos(halfSectorAngle * Mathf.Deg2Rad);

        return dotProduct >= cosHalfAngle;
    }
    #endregion

    #region Private Methods - Registration
    private void RegisterEnemy(IBattleEntity enemy)
    {
        if (enemy == null || !enemy.IsAlive) return;

        _registeredEnemies.Add(enemy);
    }

    private void UnregisterEnemy(IBattleEntity enemy)
    {
        if (enemy == null) return;

        _registeredEnemies.Remove(enemy);
        _detectedEnemies.Remove(enemy);
    }

    private void ClearInvalidEnemies()
    {
        _registeredEnemies.RemoveWhere(enemy => enemy == null || !enemy.IsAlive);
        _detectedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeComponent()
    {
        _registeredEnemies = new HashSet<IBattleEntity>();
        _detectedEnemies = new List<IBattleEntity>();
        _tempDetectedList = new List<IBattleEntity>();

        // 초기 콜라이더 크기 설정
        if (ValidateColliderForAutoSizing())
        {
            UpdateColliderSize();
        }
    }

    private void ValidateReferences()
    {
        if (_sectorSettings == null)
        {
            Debug.LogError("[TurretSectorDetection] TurretSectorSettings reference missing!", this);
            return;
        }

        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null)
            {
                Debug.LogError("[TurretSectorDetection] No trigger collider found!", this);
                return;
            }
        }

        if (!_triggerCollider.isTrigger)
        {
            Debug.LogWarning("[TurretSectorDetection] Collider should be set as trigger", this);
            _triggerCollider.isTrigger = true;
        }
    }

    private void SubscribeToSettings()
    {
        if (_sectorSettings != null)
        {
            _sectorSettings.OnSettingsChanged += OnSettingsChanged;
        }
    }

    private void UnsubscribeFromSettings()
    {
        if (_sectorSettings != null)
        {
            _sectorSettings.OnSettingsChanged -= OnSettingsChanged;
        }
    }

    private void OnSettingsChanged(TurretSectorSettings settings)
    {
        RefreshDetectionArea();

        // 설정 변경에 따른 콜라이더 크기 자동 조정
        UpdateColliderSize();
    }
    #endregion
}