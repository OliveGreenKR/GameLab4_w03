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
    [Header("Layer Filtering")]
    [InfoBox("탐지할 적 레이어 마스크")]
    [SerializeField] private LayerMask _enemyLayerMask = -1;

    [TabGroup("Detection")]
    [Header("Detection Buffer")]
    [InfoBox("OverlapSphere 결과 캐싱 배열 크기")]
    [SuffixLabel("colliders")]
    [PropertyRange(10, 100)]
    [SerializeField] private int _colliderBufferSize = 100;

    [TabGroup("Performance")]
    [Header("Update Settings")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _filterUpdateInterval = 0.1f;

    [TabGroup("Performance")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _overlapDetectionInterval = 0.05f;
    #endregion

    #region Properties

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
    private Collider[] _colliderBuffer;
    private List<IBattleEntity> _detectedEnemies = new List<IBattleEntity>();
    private List<IBattleEntity> _tempDetectedList = new List<IBattleEntity>();
    private float _nextOverlapTime;
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

        _nextOverlapTime = Time.time + _overlapDetectionInterval;
        NextFilterTime = Time.time + _filterUpdateInterval;
        IsInitialized = true;
    }

    private void Update()
    {
        if (!IsInitialized) return;

        // OverlapSphere 주기적 실행
        if (Time.time >= _nextOverlapTime)
        {
            PerformOverlapDetection();
            _nextOverlapTime = Time.time + _overlapDetectionInterval;
        }

        // 필터링 업데이트
        if (Time.time >= NextFilterTime)
        {
            UpdateDetectionFilter();
            NextFilterTime = Time.time + _filterUpdateInterval;
        }
    }

    private void OnValidate()
    {
        _filterUpdateInterval = Mathf.Clamp(_filterUpdateInterval, 0.05f, 0.2f);
        _overlapDetectionInterval = Mathf.Clamp(_overlapDetectionInterval, 0.02f, 0.1f);
        _colliderBufferSize = Mathf.Clamp(_colliderBufferSize, 10, 100);
    }

    private void OnDestroy()
    {
        UnsubscribeFromSettings();
    }
    #endregion

    #region Private Methods - Collider Management
    private float CalculateColliderRadius()
    {
        if (_sectorSettings == null)
            return 1f;

        float baseRadius = _sectorSettings.DetectionRadius;
        float scaledRadius = baseRadius;

        // 안전한 범위로 제한
        return Mathf.Clamp(scaledRadius, 1f, 100f);
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
    private void PerformOverlapDetection()
    {
        if (_sectorSettings == null || _colliderBuffer == null)
            return;

        Vector3 detectionCenter = DetectionCenter;
        float detectionRadius = _sectorSettings.DetectionRadius;

        int hitCount = Physics.OverlapSphereNonAlloc(
            detectionCenter,
            detectionRadius,
            _colliderBuffer,
            _enemyLayerMask
        );

        _tempDetectedList.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _colliderBuffer[i];
            if (hitCollider == null) continue;

            IBattleEntity enemy = hitCollider.GetComponent<IBattleEntity>();
            if (enemy != null && enemy.IsAlive && IsEnemyInSector(enemy))
            {
                _tempDetectedList.Add(enemy);
            }
        }

        _detectedEnemies.Clear();
        _detectedEnemies.AddRange(_tempDetectedList);
    }

    private void UpdateDetectionFilter()
    {
        if (_sectorSettings == null) return;

        ClearInvalidEnemies();
    }

    private bool IsEnemyInSector(IBattleEntity enemy)
    {
        if (enemy == null || !enemy.IsAlive) return false;

        return IsEnemyInRange(enemy) && IsEnemyInAngle(enemy);
    }

    private bool IsEnemyInRange(IBattleEntity enemy)
    {
        Vector3 toEnemy = enemy.Transform.position - DetectionCenter;
        toEnemy.y = 0f;

        float sqrDistance = toEnemy.sqrMagnitude;
        float detectionRadius = _sectorSettings.DetectionRadius;

        return sqrDistance <= (detectionRadius * detectionRadius);
    }

    private bool IsEnemyInAngle(IBattleEntity enemy)
    {
        Vector3 toEnemy = enemy.Transform.position - DetectionCenter;
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude < KINDA_SMALL) return true;

        Vector3 forward = DetectionForward;
        forward.y = 0f;

        if (forward.sqrMagnitude < KINDA_SMALL) return false;

        float dotProduct = Vector3.Dot(forward.normalized, toEnemy.normalized);
        float halfSectorAngle = _sectorSettings.SectorAngleDegrees * 0.5f;
        float cosHalfAngle = Mathf.Cos(halfSectorAngle * Mathf.Deg2Rad);

        return dotProduct >= cosHalfAngle;
    }

    private void ClearInvalidEnemies()
    {
        _detectedEnemies.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeComponent()
    {
        _detectedEnemies = new List<IBattleEntity>();
        _tempDetectedList = new List<IBattleEntity>();

        // OverlapSphere 결과 캐싱 배열 초기화
        _colliderBuffer = new Collider[_colliderBufferSize];
    }

    private void ValidateReferences()
    {
        if (_sectorSettings == null)
        {
            Debug.LogError("[TurretSectorDetection] TurretSectorSettings reference missing!", this);
            return;
        }

        if (_colliderBuffer == null || _colliderBuffer.Length != _colliderBufferSize)
        {
            Debug.LogWarning("[TurretSectorDetection] Collider buffer not properly initialized", this);
            _colliderBuffer = new Collider[_colliderBufferSize];
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
    }
    #endregion
}