using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 터렛 타겟 선택 및 관리 컴포넌트
/// TurretSectorDetection에서 탐지된 적들 중 최적 타겟 선택
/// </summary>
public class TurretTargeting : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Detection Component")]
    [Required]
    [SerializeField] private TurretSectorDetection _sectorDetection;

    [TabGroup("Settings")]
    [Header("Team Settings")]
    [SerializeField] private int _turretTeamId = 0;

    [TabGroup("Settings")]
    [Header("Target Validation")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.05f, 0.5f)]
    [SerializeField] private float _targetValidationInterval = 0.1f;

    [TabGroup("Settings")]
    [Header("Target Selection")]
    [InfoBox("타겟 선택 우선순위 기준")]
    [SerializeField] private bool _prioritizeClosest = true;

    [TabGroup("Settings")]
    [Header("Target Selection")]
    [InfoBox("타겟 교체 우선순위 threshold (타겟 교체 진동 방지용)")]
    [SuffixLabel("Multiplier")]
    [SerializeField] private float _priorityThreshold = 1.2f;

    [TabGroup("Settings")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.5f, 5f)]
    [SerializeField] private float _targetLossTimeout = 2f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public IBattleEntity CurrentTarget { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasValidTarget => CurrentTarget != null && CurrentTarget.IsAlive;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsManualTargeting { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int AvailableTargetCount { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float TimeSinceLastValidation { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DistanceToCurrentTarget { get; private set; }
    #endregion

    #region Events
    /// <summary>타겟 변경 시 발생 (이전, 새로운)</summary>
    public event Action<IBattleEntity, IBattleEntity> OnTargetChanged;
    #endregion

    #region Private Fields
    private float _nextValidationTime;
    private IBattleEntity _previousTarget;
    private float _targetLossTimer;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponent();
        ValidateReferences();
        SubscribeToEvents();

        _nextValidationTime = Time.time + _targetValidationInterval;
        _targetLossTimer = 0f;
        TimeSinceLastValidation = 0f;

        Debug.Log("[TurretTargeting] Component initialized", this);
    }

    private void Update()
    {
        TimeSinceLastValidation += Time.deltaTime;

        if (Time.time >= _nextValidationTime)
        {
            if (!IsManualTargeting)
            {
                UpdateTargetSelection();
            }

            ValidateCurrentTarget();
            UpdateTargetDistance();

            _nextValidationTime = Time.time + _targetValidationInterval;
            TimeSinceLastValidation = 0f;
        }

        // 타겟 상실 타이머 업데이트
        if (!HasValidTarget && _targetLossTimer > 0f)
        {
            _targetLossTimer -= Time.deltaTime;
            if (_targetLossTimer <= 0f)
            {
                HandleTargetLoss();
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (CurrentTarget != null)
        {
            ClearTarget();
        }
    }

    private void OnValidate()
    {

        if (Application.isPlaying && Time.time > 0f)
        {
            _nextValidationTime = Time.time + _targetValidationInterval;
        }
    }
    #endregion

    #region Public Methods - Target Control
    /// <summary>자동 타겟 선택 시작</summary>
    public void StartAutoTargeting()
    {
        if (IsManualTargeting)
        {
            IsManualTargeting = false;
            Debug.Log("[TurretTargeting] Switched to auto targeting mode", this);
        }

        ForceTargetSelection();
    }

    /// <summary>수동 타겟 지정</summary>
    /// <param name="target">지정할 타겟</param>
    public void SetManualTarget(IBattleEntity target)
    {
        if (target == null)
        {
            Debug.LogWarning("[TurretTargeting] Cannot set null manual target", this);
            return;
        }

        if (!IsValidTarget(target))
        {
            Debug.LogWarning("[TurretTargeting] Invalid manual target specified", this);
            return;
        }

        IsManualTargeting = true;
        SetCurrentTarget(target);

        Debug.Log($"[TurretTargeting] Manual target set: {target.GameObject.name}", this);
    }

    /// <summary>현재 타겟 해제</summary>
    public void ClearTarget()
    {
        if (CurrentTarget != null)
        {
            IBattleEntity previousTarget = CurrentTarget;
            SetCurrentTarget(null);

            Debug.Log($"[TurretTargeting] Target cleared: {previousTarget.GameObject.name}", this);
        }

        _targetLossTimer = 0f;
    }

    /// <summary>타겟 선택 즉시 실행</summary>
    public void ForceTargetSelection()
    {
        if (IsManualTargeting)
        {
            Debug.LogWarning("[TurretTargeting] Cannot force selection in manual targeting mode", this);
            return;
        }

        IBattleEntity bestTarget = SelectBestTarget();
        SetCurrentTarget(bestTarget);

        _nextValidationTime = Time.time + _targetValidationInterval;
    }

    /// <summary>수동 타겟 모드 해제</summary>
    public void ClearManualTargeting()
    {
        if (IsManualTargeting)
        {
            IsManualTargeting = false;
            Debug.Log("[TurretTargeting] Manual targeting mode cleared", this);

            // 자동 타겟 선택으로 전환
            ForceTargetSelection();
        }
    }
    #endregion

    #region Public Methods - Query
    /// <summary>현재 타겟까지의 거리</summary>
    /// <returns>거리 (미터)</returns>
    public float GetDistanceToTarget()
    {
        if (!HasValidTarget)
            return float.MaxValue;

        return DistanceToCurrentTarget;
    }

    /// <summary>타겟 방향 벡터</summary>
    /// <returns>정규화된 방향 벡터</returns>
    public Vector3 GetDirectionToTarget()
    {
        if (!HasValidTarget)
            return transform.forward;

        Vector3 direction = CurrentTarget.Transform.position - transform.position;
        direction.y = 0f; // Y축 무시

        return direction.normalized;
    }

    /// <summary>타겟이 사정거리 내에 있는지 확인</summary>
    /// <param name="maxRange">최대 사정거리</param>
    /// <returns>사정거리 내 여부</returns>
    public bool IsTargetInRange(float maxRange)
    {
        if (!HasValidTarget || maxRange <= 0f)
            return false;

        return DistanceToCurrentTarget <= maxRange;
    }
    #endregion

    #region Private Methods - Target Selection
    private IBattleEntity SelectBestTarget()
    {
        if (_sectorDetection == null)
            return null;

        var detectedEnemies = _sectorDetection.GetDetectedEnemies();
        AvailableTargetCount = detectedEnemies.Count;

        if (AvailableTargetCount == 0)
            return null;

        IBattleEntity bestTarget = null;
        float bestPriority = float.MinValue;

        foreach (var enemy in detectedEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            float priority = CalculateTargetPriority(enemy);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private float CalculateTargetPriority(IBattleEntity target)
    {
        if (target == null || !target.IsAlive)
            return float.MinValue;

        Vector3 toTarget = target.Transform.position - transform.position;
        toTarget.y = 0f; // Y축 무시
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
            return float.MaxValue; // 매우 가까운 타겟

        // 거리 기반 우선순위 (가까울수록 높음)
        if (_prioritizeClosest)
        {
            return 1000f / distance; // 거리 역수 기반
        }

        return distance; // 먼 타겟 우선 (특수 상황용)
    }

    private bool IsValidTarget(IBattleEntity target)
    {
        if (target == null)
            return false;

        if (!target.IsAlive)
            return false;

        if (target.GameObject == null)
            return false;

        // 같은 팀 검증 (같은 팀이면 타겟 불가)
        if (target.TeamId == _turretTeamId)
            return false;

        return true;
    }

    private void UpdateTargetSelection()
    {
        IBattleEntity newBestTarget = SelectBestTarget();

        // 현재 타겟이 여전히 유효하고 최적인지 확인
        if (CurrentTarget != null && IsValidTarget(CurrentTarget))
        {
            // 현재 타겟 우선순위와 새 타겟 비교
            float currentPriority = CalculateTargetPriority(CurrentTarget);
            float newPriority = newBestTarget != null ? CalculateTargetPriority(newBestTarget) : float.MinValue;

            // 현재 타겟이 여전히 충분히 좋으면 유지 (떨림 방지)
            if (currentPriority * _priorityThreshold >= newPriority)
            {
                return;
            }
        }

        // 타겟 변경
        SetCurrentTarget(newBestTarget);
    }
    #endregion

    #region Private Methods - Target Management
    private void SetCurrentTarget(IBattleEntity newTarget)
    {
        IBattleEntity previousTarget = CurrentTarget;

        if (previousTarget == newTarget)
            return;

        CurrentTarget = newTarget;
        _previousTarget = previousTarget;

        if (newTarget != null)
        {
            _targetLossTimer = 0f;
            UpdateTargetDistance();
        }
        else
        {
            DistanceToCurrentTarget = float.MaxValue;
            _targetLossTimer = _targetLossTimeout;
        }

        // 타겟 변경 이벤트 발생
        OnTargetChanged?.Invoke(previousTarget, newTarget);
    }

    private void ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
            return;

        if (!IsValidTarget(CurrentTarget))
        {
            SetCurrentTarget(null);
            return;
        }

        // 탐지 범위 내에 있는지 확인
        if (_sectorDetection != null && !_sectorDetection.IsEnemyDetected(CurrentTarget))
        {
            SetCurrentTarget(null);
        }
    }

    private void UpdateTargetDistance()
    {
        if (!HasValidTarget)
        {
            DistanceToCurrentTarget = float.MaxValue;
            return;
        }

        Vector3 toTarget = CurrentTarget.Transform.position - transform.position;
        toTarget.y = 0f; // Y축 무시
        DistanceToCurrentTarget = toTarget.magnitude;
    }

    private void HandleTargetLoss()
    {
        _targetLossTimer = 0f;

        if (!IsManualTargeting)
        {
            // 자동 모드에서는 즉시 새 타겟 탐색
            ForceTargetSelection();
        }
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeComponent()
    {
        CurrentTarget = null;
        _previousTarget = null;
        IsManualTargeting = false;
        AvailableTargetCount = 0;
        DistanceToCurrentTarget = float.MaxValue;
        _targetLossTimer = 0f;

        Debug.Log("[TurretTargeting] Component state initialized", this);
    }

    private void ValidateReferences()
    {
        if (_sectorDetection == null)
        {
            _sectorDetection = GetComponent<TurretSectorDetection>();
            if (_sectorDetection == null)
            {
                Debug.LogError("[TurretTargeting] TurretSectorDetection component not found!", this);
            }
            else
            {
                Debug.LogWarning("[TurretTargeting] SectorDetection auto-assigned from same GameObject", this);
            }
        }

        if (_sectorDetection != null && !_sectorDetection.IsInitialized)
        {
            Debug.LogWarning("[TurretTargeting] SectorDetection not yet initialized", this);
        }
    }

    private void SubscribeToEvents()
    {
        // 현재는 구독할 이벤트 없음 (polling 방식 사용)
        // 필요시 SectorDetection 이벤트 구독 가능
    }

    private void UnsubscribeFromEvents()
    {
        // 현재는 구독 해제할 이벤트 없음
        // 필요시 SectorDetection 이벤트 구독 해제
    }
    #endregion
}