using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ISpawnable을 구현하는 적 배틀 엔티티
/// Spawner로부터 받은 스탯을 BattleStatComponent와 연동하여 관리
/// </summary>
public class EnemyBattleEntity : BaseBattleEntity, ISpawnable
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Team Settings")]
    [InfoBox("적 엔티티의 팀 ID입니다. 모든 스탯은 Spawner에서 결정됩니다.")]
    [SerializeField] private int _enemyTeamId = 1;
    #endregion

    #region ISpawnable Events
    /// <summary>
    /// 스폰 완료 시 발생하는 이벤트
    /// </summary>
    public event Action<ISpawnable> OnSpawnCompleted;

    /// <summary>
    /// 디스폰 시작 시 발생하는 이벤트
    /// </summary>
    public event Action<ISpawnable> OnDespawnStarted;

    /// <summary>
    /// 스폰 스탯 변경 시 발생하는 이벤트
    /// </summary>
    public event Action<ISpawnable, SpawnStatType, float> OnSpawnStatChanged;
    #endregion

    #region ISpawnable Properties
    public new Transform Transform => transform;
    public new GameObject GameObject => gameObject;
    public bool IsSpawned => _isSpawned;
    public bool CanBeSpawned => _canBeSpawned && _battleStat != null;
    public bool ShouldReturnToPool => !IsAlive || _shouldReturnToPool;
    #endregion

    #region Debug Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("Spawner에서 설정된 이동속도")]
    public float CurrentMoveSpeed => _currentMoveSpeed;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public object CurrentSpawner => _spawner;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasPoolCallback => _poolReturnCallback != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("현재 적용된 스폰 스탯 현황")]
    public Dictionary<SpawnStatType, float> CurrentSpawnStats => GetCurrentSpawnStatsDebug();
    #endregion

    #region Private Fields - Spawn State
    private bool _isSpawned = false;
    private bool _canBeSpawned = true;
    private bool _shouldReturnToPool = false;

    // 스폰 스탯 관리 (MoveSpeed는 BattleStatType에 없으므로 별도 관리)
    private float _currentMoveSpeed = 0f;

    // 스폰너 관리
    private object _spawner;

    // 풀링 시스템 연동
    private Action<ISpawnable> _poolReturnCallback;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();

        // 스폰 상태 초기화
        _currentMoveSpeed = 0f; // 스폰되지 않은 상태
        _isSpawned = false;
        _canBeSpawned = true;
        _shouldReturnToPool = false;
        _spawner = null;
        _poolReturnCallback = null;

        // BattleStatComponent 검증
        if (_battleStat == null)
        {
            Debug.LogError("[EnemyBattleEntity] BattleStatComponent is required!", this);
            _canBeSpawned = false;
        }
    }

    protected override void Start()
    {
        base.Start();

        // 팀 ID 설정 (적 팀)
        if (_battleStat != null)
        {
            _battleStat.SetTeamId(_enemyTeamId);
        }

        // 초기 상태 검증
        ValidateSpawnState();

        // 스폰되지 않은 상태에서는 BattleStatData 기본값으로 유지
        if (!_isSpawned)
        {
            ResetToBattleStatDefaults();
        }
    }

    protected override void Update()
    {
        base.Update();

        // 스폰 상태 유효성 지속 검사
        if (_isSpawned)
        {
            // 죽었거나 비활성화되면 풀 반환 대상으로 표시
            if (!IsAlive || !gameObject.activeInHierarchy)
            {
                _shouldReturnToPool = true;
            }

            // BattleStat이 손상되면 스폰 불가능 상태로 변경
            if (_battleStat == null)
            {
                _canBeSpawned = false;
                _shouldReturnToPool = true;
            }
        }
    }
    #endregion

    #region ISpawnable Implementation - Position and Rotation
    /// <summary>
    /// 스폰 위치 설정
    /// </summary>
    /// <param name="worldPosition">월드 좌표 기준 위치</param>
    public void SetSpawnPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    /// <summary>
    /// 스폰 회전 설정
    /// </summary>
    /// <param name="worldRotation">월드 좌표 기준 회전</param>
    public void SetSpawnRotation(Quaternion worldRotation)
    {
        transform.rotation = worldRotation;
    }

    /// <summary>
    /// 스폰 위치와 회전 동시 설정
    /// </summary>
    /// <param name="worldPosition">월드 좌표 기준 위치</param>
    /// <param name="worldRotation">월드 좌표 기준 회전</param>
    public void SetSpawnTransform(Vector3 worldPosition, Quaternion worldRotation)
    {
        transform.position = worldPosition;
        transform.rotation = worldRotation;
    }
    #endregion

    #region ISpawnable Implementation - Spawn Stats Management
    /// <summary>
    /// 특정 스폰 스탯 값 조회
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>스탯 값</returns>
    public float GetSpawnStat(SpawnStatType statType)
    {
        if (_battleStat == null)
            return 0f;

        switch (statType)
        {
            case SpawnStatType.Health:
                return _battleStat.GetCurrentStat(BattleStatType.Health);
            case SpawnStatType.Attack:
                return _battleStat.GetCurrentStat(BattleStatType.Attack);
            case SpawnStatType.MoveSpeed:
                return _currentMoveSpeed;
            default:
                Debug.LogWarning($"[EnemyBattleEntity] Unknown SpawnStatType: {statType}", this);
                return 0f;
        }
    }

    /// <summary>
    /// 특정 스폰 스탯 값 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 값</param>
    public void SetSpawnStat(SpawnStatType statType, float value)
    {
        if (_battleStat == null)
        {
            Debug.LogWarning("[EnemyBattleEntity] BattleStatComponent is null", this);
            return;
        }

        float clampedValue = Mathf.Max(0f, value);

        switch (statType)
        {
            case SpawnStatType.Health:
                //ISpawnable 스탯 변경 시 최대 체력과 현재 체력을 동일하게 설정
                _battleStat.SetCurrentStat(BattleStatType.MaxHealth, clampedValue);
                _battleStat.SetCurrentStat(BattleStatType.Health, clampedValue);
                break;
            case SpawnStatType.Attack:
                _battleStat.SetCurrentStat(BattleStatType.Attack, clampedValue);
                break;
            case SpawnStatType.MoveSpeed:
                _currentMoveSpeed = clampedValue;
                break;
            default:
                Debug.LogWarning($"[EnemyBattleEntity] Unknown SpawnStatType: {statType}", this);
                return;
        }

        TriggerSpawnStatChanged(statType, clampedValue);
    }

    /// <summary>
    /// 여러 스폰 스탯을 한번에 적용
    /// </summary>
    /// <param name="stats">적용할 스탯 딕셔너리</param>
    public void ApplySpawnStats(Dictionary<SpawnStatType, float> stats)
    {
        if (stats == null)
        {
            Debug.LogWarning("[EnemyBattleEntity] Spawn stats dictionary is null", this);
            return;
        }

        if (_battleStat == null)
        {
            Debug.LogWarning("[EnemyBattleEntity] BattleStatComponent is null", this);
            return;
        }

        // 스폰 스탯을 배틀 스탯으로 동기화
        SyncSpawnStatsToBattleStats(stats);

        Debug.Log($"[EnemyBattleEntity] Applied spawn stats: " +
                 $"Health={GetSpawnStat(SpawnStatType.Health):F1}, " +
                 $"Attack={GetSpawnStat(SpawnStatType.Attack):F1}, " +
                 $"MoveSpeed={GetSpawnStat(SpawnStatType.MoveSpeed):F1}", this);
    }
    #endregion

    #region ISpawnable Implementation - Spawn Lifecycle
    /// <summary>
    /// 스폰 전 초기화 작업
    /// </summary>
    public void PreSpawnInitialize()
    {
        // 스폰 상태 초기화
        _isSpawned = false;
        _shouldReturnToPool = false;

        // BattleStatData 기본값으로 리셋
        ResetToBattleStatDefaults();

        // 스폰 가능 상태 검증
        ValidateSpawnState();

        Debug.Log("[EnemyBattleEntity] Pre-spawn initialization completed", this);
    }

    /// <summary>
    /// 스폰 완료 시 호출
    /// </summary>
    /// <param name="spawner">스폰을 실행한 스폰너 객체</param>
    public void OnSpawned(object spawner = null)
    {
        // 스폰 상태 업데이트
        _isSpawned = true;
        _shouldReturnToPool = false;

        // 스폰너 설정
        SetSpawner(spawner);

        // 오브젝트 활성화
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        // 스폰 완료 이벤트 발생
        OnSpawnCompleted?.Invoke(this);

        Debug.Log($"[EnemyBattleEntity] Spawned by {spawner?.GetType().Name ?? "Unknown"} - " +
                 $"HP: {GetSpawnStat(SpawnStatType.Health):F1}, " +
                 $"Attack: {GetSpawnStat(SpawnStatType.Attack):F1}, " +
                 $"Speed: {GetSpawnStat(SpawnStatType.MoveSpeed):F1}", this);
    }

    /// <summary>
    /// 스폰 후 추가 초기화 작업
    /// </summary>
    public void PostSpawnInitialize()
    {
        // 현재는 추가 초기화 작업 없음
        // 필요시 하위 클래스에서 오버라이드하거나 이곳에 추가

        Debug.Log("[EnemyBattleEntity] Post-spawn initialization completed", this);
    }

    /// <summary>
    /// 디스폰 시 호출
    /// </summary>
    public void OnDespawned()
    {
        // 디스폰 시작 이벤트 발생
        OnDespawnStarted?.Invoke(this);

        // 스폰 상태 업데이트
        _isSpawned = false;
        _shouldReturnToPool = true;

        // 스폰너 참조 해제
        _spawner = null;

        Debug.Log("[EnemyBattleEntity] Despawned", this);
    }

    /// <summary>
    /// 스폰 기본값으로 리셋
    /// </summary>
    public void ResetToSpawnDefaults()
    {
        // BattleStatData 기본값으로 리셋
        ResetToBattleStatDefaults();

        // MoveSpeed는 스폰되지 않은 상태로 초기화
        _currentMoveSpeed = 0f;

        // 스폰 상태 초기화
        _isSpawned = false;
        _shouldReturnToPool = false;
        _spawner = null;

        // 스폰 가능 상태로 복구
        _canBeSpawned = (_battleStat != null);

        Debug.Log("[EnemyBattleEntity] Reset to spawn defaults", this);
    }
    #endregion

    #region ISpawnable Implementation - Spawner Management
    /// <summary>
    /// 스폰너 설정
    /// </summary>
    /// <param name="spawner">스폰너 객체</param>
    public void SetSpawner(object spawner)
    {
        _spawner = spawner;
    }

    /// <summary>
    /// 현재 스폰너 조회
    /// </summary>
    /// <returns>스폰너 객체</returns>
    public object GetSpawner()
    {
        return _spawner;
    }
    #endregion

    #region ISpawnable Implementation - Pool Synchronization
    /// <summary>
    /// 풀로 반환될 때 호출되는 정리 작업
    /// </summary>
    public void OnReturnToPool()
    {
        // 스폰 상태 정리
        _isSpawned = false;
        _shouldReturnToPool = false;
        _spawner = null;

        // 이벤트 정리
        ClearSpawnEvents();

        // 오브젝트 비활성화
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }

        // 풀 반환 콜백 호출
        if (_poolReturnCallback != null)
        {
            _poolReturnCallback.Invoke(this);
            _poolReturnCallback = null;
        }

        Debug.Log("[EnemyBattleEntity] Returned to pool", this);
    }

    /// <summary>
    /// 풀에서 가져올 때 호출되는 초기화 작업
    /// </summary>
    public void OnRetrievedFromPool()
    {
        // 스폰 가능 상태로 초기화
        _canBeSpawned = (_battleStat != null);
        _shouldReturnToPool = false;
        _isSpawned = false;

        // BattleStatData 기본값으로 리셋
        ResetToBattleStatDefaults();

        // 스폰 상태 검증
        ValidateSpawnState();

        Debug.Log("[EnemyBattleEntity] Retrieved from pool", this);
    }

    /// <summary>
    /// 풀 반환 콜백 등록
    /// </summary>
    /// <param name="returnCallback">풀 반환 시 호출될 콜백</param>
    public void RegisterPoolReturnCallback(Action<ISpawnable> returnCallback)
    {
        _poolReturnCallback = returnCallback;
    }
    #endregion

    #region BaseBattleEntity Overrides
    public override void OnDeath(IBattleEntity killer = null)
    {
        base.OnDeath(killer);

        // 디스폰 처리
        OnDespawned();

        // 즉시 풀 반환 처리
        OnReturnToPool();

        Debug.Log($"[EnemyBattleEntity] Death processed and returned to pool. Killer: {killer?.GameObject.name ?? "Unknown"}", this);
    }
    #endregion

    #region Private Methods - Stat Synchronization
    /// <summary>
    /// 스폰 스탯을 배틀 스탯으로 동기화
    /// </summary>
    /// <param name="spawnStats">동기화할 스폰 스탯</param>
    private void SyncSpawnStatsToBattleStats(Dictionary<SpawnStatType, float> spawnStats)
    {
        if (_battleStat == null || spawnStats == null)
            return;

        foreach (var kvp in spawnStats)
        {
            SpawnStatType statType = kvp.Key;
            float value = Mathf.Max(0f, kvp.Value);

            switch (statType)
            {
                case SpawnStatType.Health:
                    _battleStat.SetCurrentStat(BattleStatType.MaxHealth, value);
                    _battleStat.SetCurrentStat(BattleStatType.Health, value);
                    break;
                case SpawnStatType.Attack:
                    _battleStat.SetCurrentStat(BattleStatType.Attack, value);
                    break;
                case SpawnStatType.MoveSpeed:
                    _currentMoveSpeed = value;
                    break;
                default:
                    Debug.LogWarning($"[EnemyBattleEntity] Unknown SpawnStatType: {statType}", this);
                    continue;
            }

            TriggerSpawnStatChanged(statType, value);
        }
    }

    /// <summary>
    /// BattleStatData 기본값으로 리셋 (스폰되지 않은 상태)
    /// </summary>
    private void ResetToBattleStatDefaults()
    {
        if (_battleStat == null)
            return;

        // BattleStatData의 기본값으로 완전 초기화
        _battleStat.InitializeStats();

        // 팀 ID 재설정 (InitializeStats에서 리셋될 수 있음)
        _battleStat.SetTeamId(_enemyTeamId);

        // MoveSpeed는 스폰되지 않은 상태로 초기화
        _currentMoveSpeed = 0f;
    }

    /// <summary>
    /// 스폰 상태 유효성 검사
    /// </summary>
    private void ValidateSpawnState()
    {
        // BattleStatComponent 검증
        if (_battleStat == null)
        {
            _canBeSpawned = false;
            Debug.LogError("[EnemyBattleEntity] BattleStatComponent is null - cannot spawn", this);
            return;
        }

        // 게임 오브젝트 활성 상태 검증
        if (!gameObject.activeInHierarchy && _isSpawned)
        {
            Debug.LogWarning("[EnemyBattleEntity] GameObject is inactive but marked as spawned", this);
            _shouldReturnToPool = true;
        }

        // 스폰 가능 상태 업데이트
        _canBeSpawned = (_battleStat != null);
    }
    #endregion

    #region Private Methods - Utility
    /// <summary>
    /// 디버깅용 현재 스폰 스탯 딕셔너리 생성
    /// </summary>
    /// <returns>현재 스폰 스탯 딕셔너리</returns>
    private Dictionary<SpawnStatType, float> GetCurrentSpawnStatsDebug()
    {
        Dictionary<SpawnStatType, float> debugStats = new Dictionary<SpawnStatType, float>();

        if (_battleStat != null)
        {
            debugStats[SpawnStatType.Health] = _battleStat.GetCurrentStat(BattleStatType.Health);
            debugStats[SpawnStatType.Attack] = _battleStat.GetCurrentStat(BattleStatType.Attack);
        }
        else
        {
            debugStats[SpawnStatType.Health] = 0f;
            debugStats[SpawnStatType.Attack] = 0f;
        }

        debugStats[SpawnStatType.MoveSpeed] = _currentMoveSpeed;

        return debugStats;
    }

    /// <summary>
    /// 스폰 스탯 변경 이벤트 발생
    /// </summary>
    /// <param name="statType">변경된 스탯 타입</param>
    /// <param name="newValue">새로운 값</param>
    private void TriggerSpawnStatChanged(SpawnStatType statType, float newValue)
    {
        OnSpawnStatChanged?.Invoke(this, statType, newValue);
    }

    /// <summary>
    /// 모든 스폰 이벤트 정리
    /// </summary>
    private void ClearSpawnEvents()
    {
        OnSpawnCompleted = null;
        OnDespawnStarted = null;
        OnSpawnStatChanged = null;
    }
    #endregion
}