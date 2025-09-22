using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyBattleEntity의 이동속도를 NavMeshAgent와 동기화하는 컴포넌트
/// </summary>
public class NavMeshMovementSync : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [Required]
    [SerializeField] private EnemyBattleEntity _enemyBattleEntity;

    [Header("References")]
    [Required]
    [SerializeField] private NavMeshAgent _navMeshAgent;

    [Header("Movement Settings")]
    [PropertyRange(0.1f, 20f)]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _baseMoveSpeed = 3.5f;
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public float CurrentMoveSpeed { get; private set; }
    #endregion


    #region Unity Lifecycle
    private void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();

        if (_navMeshAgent == null)
        {
            Debug.LogError("[NavMeshMovementSync] NavMeshAgent component not found!", this);
            return;
        }

        if (_enemyBattleEntity == null)
        {
            Debug.LogError("[NavMeshMovementSync] EnemyBattleEntity not assigned!", this);
            return;
        }

        // 스폰되지 않은 상태라면 기본값 설정
        if (!_enemyBattleEntity.IsSpawned)
        {
            InitializeSync();
        }
    }

    private void OnEnable()
    {
        if (_enemyBattleEntity != null)
        {
            _enemyBattleEntity.OnSpawnStatChanged -= OnMoveSpeedChanged;
            _enemyBattleEntity.OnSpawnStatChanged += OnMoveSpeedChanged;

            // 이미 스폰된 상태라면 즉시 동기화 (이벤트 놓침 방지)
            if (_enemyBattleEntity.IsSpawned)
            {
                float currentSpeed = _enemyBattleEntity.GetSpawnStat(SpawnStatType.MoveSpeed);
                if (currentSpeed > 0f)
                {
                    CurrentMoveSpeed = currentSpeed;
                    UpdateNavMeshSpeed(currentSpeed);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (_enemyBattleEntity != null)
        {
            _enemyBattleEntity.OnSpawnStatChanged -= OnMoveSpeedChanged;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 기본 이동속도를 설정하고 동기화합니다
    /// </summary>
    /// <param name="newSpeed">새로운 기본 이동속도</param>
    public void SetBaseMoveSpeed(float newSpeed)
    {
        _baseMoveSpeed = Mathf.Clamp(newSpeed, 0.1f, 20f);

        if (_enemyBattleEntity != null)
        {
            _enemyBattleEntity.SetSpawnStat(SpawnStatType.MoveSpeed, _baseMoveSpeed);
        }
    }
    #endregion

    #region Private Methods
    private void InitializeSync()
    {
        // 기본 이동속도를 EnemyBattleEntity에 설정
        _enemyBattleEntity.SetSpawnStat(SpawnStatType.MoveSpeed, _baseMoveSpeed);

        // 현재 이동속도 업데이트
        CurrentMoveSpeed = _enemyBattleEntity.GetSpawnStat(SpawnStatType.MoveSpeed);

        // NavMeshAgent 속도 동기화
        UpdateNavMeshSpeed(CurrentMoveSpeed);

        Debug.Log($"[NavMeshMovementSync] Initialized with speed: {CurrentMoveSpeed}", this);
    }

    private void OnMoveSpeedChanged(ISpawnable spawnable, SpawnStatType statType, float newValue)
    {
        // MoveSpeed 변경만 처리
        if (statType != SpawnStatType.MoveSpeed)
            return;

        CurrentMoveSpeed = newValue;
        UpdateNavMeshSpeed(newValue);
    }
    private void UpdateNavMeshSpeed(float speed)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = speed;
        }
    }
    #endregion
}