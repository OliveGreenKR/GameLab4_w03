using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Pool")]
    [Header("Enemy Pool")]
    [Required]
    [SerializeField] private PrefabsPool _enemyPool;

    [TabGroup("Spawn Points")]
    [Header("Spawn Locations")]
    [Required]
    [SerializeField] private Transform[] _spawnPoints;

    [TabGroup("Spawn Settings")]
    [Header("Spawn Timing")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.1f, 60f)]
    [SerializeField] private float _spawnIntervalSeconds = 5f;

    [TabGroup("Spawn Settings")]
    [Header("Pack Size Range")]
    [SuffixLabel("enemies")]
    [SerializeField] private int _minPackSize = 3;

    [TabGroup("Spawn Settings")]
    [SuffixLabel("enemies")]
    [SerializeField] private int _maxPackSize = 10;

    [TabGroup("Stats")]
    [Header("Initial Stat Ranges")]
    [SerializeField] private SpawnStatRange _initialStatRange;

    [TabGroup("Difficulty")]
    [Header("Difficulty Progression")]
    [SerializeField] private DifficultyProgression _difficultyProgression;

    [TabGroup("Difficulty")]
    [Header("Max Value Upgrades")]
    [SerializeField] private WeightedMaxUpgrade[] _maxUpgrades;

    [TabGroup("Difficulty")]
    [Header("Difficulty Timing")]
    [SuffixLabel("spawns")]
    [PropertyRange(1, 50)]
    [SerializeField] private int _difficultyUpdateInterval = 5;
    #endregion

    #region Events
    /// <summary>
    /// 스폰 완료 시 발생하는 이벤트
    /// </summary>
    public event Action<int, int> OnSpawnCompleted; // (totalSpawned, currentCycle)

    /// <summary>
    /// 난이도 업그레이드 시 발생하는 이벤트
    /// </summary>
    public event Action<int> OnDifficultyUpgraded; // (newCycle)

    /// <summary>
    /// 최대값 업그레이드 시 발생하는 이벤트
    /// </summary>
    public event Action<MaxUpgradeTarget, float> OnMaxValueUpgraded; // (target, newValue)
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsSpawning { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentCycle { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int SpawnCount { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float TimeUntilNextSpawn { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public SpawnStatRange CurrentStatRange => _currentStatRange;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentMinPackSize => _currentMinPackSize;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentMaxPackSize => _currentMaxPackSize;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsPoolInitialized => _enemyPool?.IsInitialized ?? false;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int ActiveSpawnPointCount => _spawnPoints?.Length ?? 0;
    #endregion

    #region Private Fields
    private SpawnStatRange _currentStatRange;
    private int _currentMinPackSize;
    private int _currentMaxPackSize;
    private float _nextSpawnTime;
    private Dictionary<MaxUpgradeTarget, float> _currentWeights;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // 풀 초기화
        if (!ValidatePool())
        {
            Debug.LogError("[EnemySpawner] Pool validation failed!", this);
            return;
        }

        if (!_enemyPool.IsInitialized)
        {
            _enemyPool.Initialize();
        }

        // 스폰 포인트 검증
        if (!ValidateSpawnPoints())
        {
            Debug.LogError("[EnemySpawner] Spawn points validation failed!", this);
            return;
        }

        // 초기 상태 설정
        InitializeStatRanges();
        InitializeWeights();

        CurrentCycle = 1;
        SpawnCount = 0;
        _nextSpawnTime = Time.time + _spawnIntervalSeconds;

        Debug.Log($"[EnemySpawner] Initialized with {_spawnPoints.Length} spawn points", this);
    }

    private void Update()
    {
        if (!IsSpawning) return;

        UpdateSpawnTimer();
    }

    private void OnValidate()
    {
        // 스폰 간격 검증
        _spawnIntervalSeconds = Mathf.Clamp(_spawnIntervalSeconds, 0.1f, 60f);

        // 팩 사이즈 검증
        _minPackSize = Mathf.Max(1, _minPackSize);
        _maxPackSize = Mathf.Max(_minPackSize, _maxPackSize);

        // 난이도 업데이트 간격 검증
        _difficultyUpdateInterval = Mathf.Max(1, _difficultyUpdateInterval);

        // 현재 값들도 업데이트 (런타임 중이면)
        if (Application.isPlaying)
        {
            _currentMinPackSize = Mathf.Max(_minPackSize, _currentMinPackSize);
            _currentMaxPackSize = Mathf.Max(_currentMinPackSize, _currentMaxPackSize);
        }
    }
    #endregion

    #region Public Methods - Spawning
    /// <summary>
    /// 스폰 시작
    /// </summary>
    public void StartSpawning()
    {
        if (!ValidatePool() || !ValidateSpawnPoints())
        {
            Debug.LogWarning("[EnemySpawner] Cannot start spawning: validation failed", this);
            return;
        }

        IsSpawning = true;
        _nextSpawnTime = Time.time + _spawnIntervalSeconds;

        Debug.Log("[EnemySpawner] Spawning started", this);
    }

    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        IsSpawning = false;
        Debug.Log("[EnemySpawner] Spawning stopped", this);
    }

    /// <summary>
    /// 즉시 스폰 실행
    /// </summary>
    public void SpawnImmediate()
    {
        if (!ValidatePool() || !ValidateSpawnPoints())
        {
            Debug.LogWarning("[EnemySpawner] Cannot spawn immediately: validation failed", this);
            return;
        }

        ExecuteSpawn();
        _nextSpawnTime = Time.time + _spawnIntervalSeconds;
    }

    /// <summary>
    /// 스폰 간격 설정
    /// </summary>
    /// <param name="intervalSeconds">스폰 간격 (초)</param>
    public void SetSpawnInterval(float intervalSeconds)
    {
        _spawnIntervalSeconds = Mathf.Clamp(intervalSeconds, 0.1f, 60f);

        // 현재 스폰 중이면 다음 스폰 시간 조정
        if (IsSpawning)
        {
            float timeElapsed = Time.time - (_nextSpawnTime - _spawnIntervalSeconds);
            _nextSpawnTime = Time.time + Mathf.Max(0.1f, _spawnIntervalSeconds - timeElapsed);
        }

        Debug.Log($"[EnemySpawner] Spawn interval set to {_spawnIntervalSeconds:F1} seconds", this);
    }
    #endregion

    #region Public Methods - Difficulty
    /// <summary>
    /// 난이도 리셋
    /// </summary>
    public void ResetDifficulty()
    {
        // 스탯 범위 초기화
        InitializeStatRanges();

        // 팩 사이즈 초기화
        _currentMinPackSize = _minPackSize;
        _currentMaxPackSize = _maxPackSize;

        // 주기 및 카운터 리셋
        CurrentCycle = 1;
        SpawnCount = 0;

        // 가중치 초기화
        InitializeWeights();

        Debug.Log("[EnemySpawner] Difficulty reset to initial values", this);
        OnDifficultyUpgraded?.Invoke(CurrentCycle);
    }

    /// <summary>
    /// 수동 난이도 업그레이드
    /// </summary>
    public void UpgradeDifficulty()
    {
        CurrentCycle++;

        // 필수 업그레이드 적용
        ApplyMandatoryUpgrades();

        // 확률적 최대값 업그레이드 처리
        ProcessWeightedMaxUpgrades();

        // 가중치 업데이트
        UpdateUpgradeWeights();

        Debug.Log($"[EnemySpawner] Manual difficulty upgrade to cycle {CurrentCycle}", this);
        OnDifficultyUpgraded?.Invoke(CurrentCycle);
    }

    /// <summary>
    /// 특정 스탯 범위 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="minValue">최소값</param>
    /// <param name="maxValue">최대값</param>
    public void SetStatRange(SpawnStatType statType, float minValue, float maxValue)
    {
        // 값 검증
        minValue = Mathf.Max(0f, minValue);
        maxValue = Mathf.Max(minValue, maxValue);

        // 현재 스탯 범위 업데이트
        _currentStatRange.SetMinValue(statType, minValue);
        _currentStatRange.SetMaxValue(statType, maxValue);

        Debug.Log($"[EnemySpawner] {statType} range set to [{minValue:F1}, {maxValue:F1}]", this);
    }
    #endregion

    #region Private Methods - Spawn Logic
    private void UpdateSpawnTimer()
    {
        TimeUntilNextSpawn = _nextSpawnTime - Time.time;

        if (Time.time >= _nextSpawnTime)
        {
            ExecuteSpawn();
            _nextSpawnTime = Time.time + _spawnIntervalSeconds;
        }
    }

    private void ExecuteSpawn()
    {
        // 팩 사이즈 결정
        int totalEnemies = GenerateRandomPackSize();

        // 스폰 포인트에 랜덤 분배
        int[] distribution = DistributeEnemiesRandomly(totalEnemies, _spawnPoints.Length);

        // 각 스폰 포인트에서 적 생성
        int actualSpawned = 0;
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (distribution[i] > 0)
            {
                SpawnEnemiesAtPoint(_spawnPoints[i], distribution[i]);
                actualSpawned += distribution[i];
            }
        }

        // 스폰 카운트 업데이트
        SpawnCount++;

        // 로그 및 이벤트 호출
        LogSpawnInfo(actualSpawned, distribution);
        OnSpawnCompleted?.Invoke(actualSpawned, CurrentCycle);

        // 난이도 업그레이드 체크
        CheckDifficultyUpgrade();
    }

    private int[] DistributeEnemiesRandomly(int totalEnemies, int spawnPointCount)
    {
        int[] distribution = new int[spawnPointCount];

        // 완전 랜덤 분배 - 각 적을 랜덤한 스폰 포인트에 할당
        for (int i = 0; i < totalEnemies; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, spawnPointCount);
            distribution[randomIndex]++;
        }

        return distribution;
    }

    private void SpawnEnemiesAtPoint(Transform spawnPoint, int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            // 약간의 위치 오프셋 적용 (겹침 방지)
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0f,
                UnityEngine.Random.Range(-1f, 1f)
            );

            Vector3 spawnPosition = spawnPoint.position + offset;
            Quaternion spawnRotation = spawnPoint.rotation;

            ISpawnable enemy = CreateEnemyFromPool(spawnPosition, spawnRotation);
            if (enemy != null)
            {
                // 랜덤 스탯 생성 및 적용
                Dictionary<SpawnStatType, float> randomStats = GenerateRandomStats();
                enemy.ApplySpawnStats(randomStats);

                // 스폰 완료 처리
                enemy.OnSpawned(this);
            }
        }
    }

    private ISpawnable CreateEnemyFromPool(Vector3 worldPosition, Quaternion worldRotation)
    {
        GameObject enemyObject = _enemyPool.SpawnObject(PrefabType.Enemy, worldPosition, worldRotation, true);

        if (enemyObject == null)
        {
            Debug.LogWarning("[EnemySpawner] Failed to spawn enemy from pool", this);
            return null;
        }

        // ISpawnable 컴포넌트만 확인
        ISpawnable spawnable = enemyObject.GetComponent<ISpawnable>();
        if (spawnable == null)
        {
            Debug.LogError($"[EnemySpawner] Enemy {enemyObject.name} does not implement ISpawnable!", this);
            _enemyPool.ReturnObject(enemyObject);
            return null;
        }

        return spawnable;
    }
    #endregion

    #region Private Methods - Difficulty Logic
    private void CheckDifficultyUpgrade()
    {
        if (SpawnCount % _difficultyUpdateInterval == 0)
        {
            CurrentCycle++;

            // 필수 업그레이드 적용
            ApplyMandatoryUpgrades();

            // 확률적 최대값 업그레이드 처리
            ProcessWeightedMaxUpgrades();

            // 가중치 업데이트
            UpdateUpgradeWeights();

            Debug.Log($"[EnemySpawner] Auto difficulty upgrade to cycle {CurrentCycle}", this);
            OnDifficultyUpgraded?.Invoke(CurrentCycle);
        }
    }

    private void ApplyMandatoryUpgrades()
    {
        // 팩 사이즈 최소값 증가
        _currentMinPackSize += Mathf.RoundToInt(_difficultyProgression.GetPackSizeMinIncrease());
        _currentMinPackSize = Mathf.Max(_minPackSize, _currentMinPackSize);

        // 스탯 최소값들 증가
        foreach (SpawnStatType statType in System.Enum.GetValues(typeof(SpawnStatType)))
        {
            float currentMin = _currentStatRange.GetMinValue(statType);
            float increase = _difficultyProgression.GetMinIncrease(statType);
            _currentStatRange.SetMinValue(statType, currentMin + increase);
        }

        // 최대값이 최소값보다 작아지지 않도록 보정
        _currentMaxPackSize = Mathf.Max(_currentMinPackSize, _currentMaxPackSize);

        foreach (SpawnStatType statType in System.Enum.GetValues(typeof(SpawnStatType)))
        {
            float currentMin = _currentStatRange.GetMinValue(statType);
            float currentMax = _currentStatRange.GetMaxValue(statType);
            if (currentMax < currentMin)
            {
                _currentStatRange.SetMaxValue(statType, currentMin);
            }
        }
    }

    private void ProcessWeightedMaxUpgrades()
    {
        if (_maxUpgrades == null || _maxUpgrades.Length == 0) return;

        foreach (WeightedMaxUpgrade upgrade in _maxUpgrades)
        {
            if (ShouldApplyMaxUpgrade(upgrade))
            {
                ApplyMaxUpgrade(upgrade);
            }
        }
    }

    private bool ShouldApplyMaxUpgrade(WeightedMaxUpgrade upgrade)
    {
        if (!_currentWeights.ContainsKey(upgrade.target))
            return false;

        float currentWeight = _currentWeights[upgrade.target];
        float randomValue = UnityEngine.Random.Range(0f, 100f);

        return randomValue < currentWeight;
    }

    private void ApplyMaxUpgrade(WeightedMaxUpgrade upgrade)
    {
        if (upgrade.target.IsPackSizeUpgrade())
        {
            // 팩 사이즈 최대값 업그레이드
            _currentMaxPackSize += Mathf.RoundToInt(upgrade.upgradeAmount);
            Debug.Log($"[EnemySpawner] Pack size max upgraded by {upgrade.upgradeAmount:F1} to {_currentMaxPackSize}", this);
        }
        else
        {
            // 스탯 최대값 업그레이드
            SpawnStatType statType = upgrade.target.ToSpawnStatType();
            float currentMax = _currentStatRange.GetMaxValue(statType);
            float newMax = currentMax + upgrade.upgradeAmount;
            _currentStatRange.SetMaxValue(statType, newMax);

            Debug.Log($"[EnemySpawner] {statType} max upgraded by {upgrade.upgradeAmount:F1} to {newMax:F1}", this);
            OnMaxValueUpgraded?.Invoke(upgrade.target, newMax);
        }
    }

    private void UpdateUpgradeWeights()
    {
        if (_maxUpgrades == null) return;

        foreach (WeightedMaxUpgrade upgrade in _maxUpgrades)
        {
            if (_currentWeights.ContainsKey(upgrade.target))
            {
                float currentWeight = _currentWeights[upgrade.target];
                float newWeight = Mathf.Min(currentWeight + upgrade.weightIncrease, upgrade.maxWeight);
                _currentWeights[upgrade.target] = newWeight;
            }
        }
    }
    #endregion

    #region Private Methods - Stat Generation
    private Dictionary<SpawnStatType, float> GenerateRandomStats()
    {
        Dictionary<SpawnStatType, float> randomStats = new Dictionary<SpawnStatType, float>();

        // 각 스탯 타입별로 랜덤 값 생성
        foreach (SpawnStatType statType in System.Enum.GetValues(typeof(SpawnStatType)))
        {
            float randomValue = _currentStatRange.GetRandomValue(statType);
            randomStats[statType] = randomValue;
        }

        return randomStats;
    }

    private int GenerateRandomPackSize()
    {
        return UnityEngine.Random.Range(_currentMinPackSize, _currentMaxPackSize + 1);
    }

    private void InitializeStatRanges()
    {
        // 초기 스탯 범위를 현재 스탯 범위에 복사
        _currentStatRange = _initialStatRange;

        // 현재 팩 사이즈 범위 초기화
        _currentMinPackSize = _minPackSize;
        _currentMaxPackSize = _maxPackSize;

        Debug.Log($"[EnemySpawner] Stat ranges initialized - Health: [{_currentStatRange.GetMinValue(SpawnStatType.Health):F1}, {_currentStatRange.GetMaxValue(SpawnStatType.Health):F1}]", this);
    }

    private void InitializeWeights()
    {
        _currentWeights = new Dictionary<MaxUpgradeTarget, float>();

        if (_maxUpgrades == null || _maxUpgrades.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No max upgrades configured", this);
            return;
        }

        // 각 업그레이드의 초기 가중치 설정
        foreach (WeightedMaxUpgrade upgrade in _maxUpgrades)
        {
            _currentWeights[upgrade.target] = upgrade.initialWeight;
        }

        Debug.Log($"[EnemySpawner] Initialized {_currentWeights.Count} upgrade weights", this);
    }
    #endregion

    #region Private Methods - Validation
    private bool ValidateSpawnPoints()
    {
        if (_spawnPoints == null)
        {
            Debug.LogError("[EnemySpawner] Spawn points array is null!", this);
            return false;
        }

        if (_spawnPoints.Length == 0)
        {
            Debug.LogError("[EnemySpawner] No spawn points assigned!", this);
            return false;
        }

        // 각 스폰 포인트가 유효한지 확인
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (_spawnPoints[i] == null)
            {
                Debug.LogError($"[EnemySpawner] Spawn point at index {i} is null!", this);
                return false;
            }
        }

        return true;
    }

    private bool ValidatePool()
    {
        if (_enemyPool == null)
        {
            Debug.LogError("[EnemySpawner] Enemy pool is null!", this);
            return false;
        }

        return true;
    }

    private void LogSpawnInfo(int totalSpawned, int[] distribution)
    {
        string distributionInfo = "";
        for (int i = 0; i < distribution.Length; i++)
        {
            if (distribution[i] > 0)
            {
                distributionInfo += $"Point{i}:{distribution[i]} ";
            }
        }

        if (string.IsNullOrEmpty(distributionInfo))
        {
            distributionInfo = "None";
        }

        Debug.Log($"[EnemySpawner] Spawn #{SpawnCount} - Total: {totalSpawned}, Distribution: {distributionInfo.Trim()}, Cycle: {CurrentCycle}", this);
    }
    #endregion
}