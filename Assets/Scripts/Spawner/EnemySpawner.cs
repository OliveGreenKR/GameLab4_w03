using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// <summary>
/// 적 타입별 스폰 설정
/// </summary>
[Serializable]
public struct EnemySpawnConfig
{
    [Header("Enemy Type")]
    public PrefabType enemyType;

    [Header("Stat Range")]
    public SpawnStatRange statRange;

    [Header("Spawn Probability")]
    [SuffixLabel("weight")]
    [PropertyRange(0.1f, 10f)]
    public float spawnWeight;

    public float GetCurrentWeight(int currentCycle)
    {
        return spawnWeight;
    }
}

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
    [SerializeField] private float _spawnIntervalSeconds = 5f;

    [TabGroup("Spawn Settings")]
    [Header("Pack Size Range")]
    [SuffixLabel("enemies")]
    [SerializeField] private int _minPackSize = 3;

    [TabGroup("Spawn Settings")]
    [SuffixLabel("enemies")]
    [SerializeField] private int _maxPackSize = 10;

    [TabGroup("Stats")]
    [Header("Enemy Type Configurations")]
    [InfoBox("각 적 타입의 스탯 범위와 스폰 가중치를 설정하세요.")]
    [SerializeField] private EnemySpawnConfig[] _enemyConfigs;

    [TabGroup("Difficulty")]
    [Header("Hybrid Difficulty System")]
    [InfoBox("타입별 고정값/% 방식 선택 가능한 하이브리드 난이도 시스템")]
    [SerializeField] private HybridDifficultyProgression _hybridDifficulty;

    [TabGroup("Difficulty")]
    [Header("Weight-Based Max Value Upgrades")]
    [InfoBox("가중치에 따라 매 주기마다 하나의 최대값 업그레이드가 선택됩니다.")]
    [SerializeField] private WeightBasedUpgradeCollection _maxUpgradeCollection;

    [TabGroup("Difficulty")]
    [Header("Difficulty Timing")]
    [SuffixLabel("spawns")]
    [SerializeField] private int _difficultyUpdateInterval = 5;
    #endregion

    #region Serialized Funcitons for Debug

    [ButtonGroup("DebugSpawns")]
    [Button(ButtonSizes.Large)]
    [GUIColor(0.4f, 0.4f, 1f)]
    private void SpawnImmediateButton() => SpawnImmediate();

    [ButtonGroup("DebugSpawns")]
    [GUIColor(0.4f, 0.4f, 1f)]
    private void StartSpawn() => StartSpawning();

    [ButtonGroup("DebugSpawns")]
    [GUIColor(0.4f, 0.4f, 1f)]
    private void StopSpawn() => StopSpawning();


    [ButtonGroup("DebugDifficulty")]
    [Button(ButtonSizes.Large)]
    [GUIColor(0.4f, 1f, 0.4f)]
    private void DifficultyUpgrade() => UpgradeDifficulty();

    [ButtonGroup("DebugDifficulty")]
    [GUIColor(0.4f, 1f, 0.4f)]
    private void ResetDiffy() => ResetDifficulty();

    #endregion

    #region Events
    /// <summary>
    /// 스폰 완료 시 발생하는 이벤트 (타입별 개수, 현재 사이클)
    /// </summary>
    public event Action<Dictionary<PrefabType, int>, int> OnSpawnCompleted;

    /// <summary>
    /// 난이도 업그레이드 시 발생하는 이벤트 (새로운 사이클 번호)
    /// </summary>
    public event Action<int> OnDifficultyUpgraded; // (newCycle)

    /// <summary>
    /// 최대값 업그레이드 시 발생하는 이벤트 (target, newValue)
    /// </summary>
    public event Action<MaxUpgradeTarget, float> OnMaxValueUpgraded; 
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
    public Dictionary<PrefabType, SpawnStatRange> CurrentStatRanges => _currentStatRanges;

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

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Dictionary<PrefabType, int> LastSpawnedEnemy { get; private set; } = new Dictionary<PrefabType, int>();

    [TabGroup("Weight System Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("현재 주기의 총 가중치 합계")]
    public float CurrentTotalWeight => _maxUpgradeCollection?.GetTotalWeight(CurrentCycle) ?? 0f;

    [TabGroup("Weight System Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("각 업그레이드별 현재 선택 확률 (%)")]
    public Dictionary<MaxUpgradeTarget, float> CurrentUpgradeProbabilities =>
        _maxUpgradeCollection?.GetSelectionProbabilities(CurrentCycle) ?? new Dictionary<MaxUpgradeTarget, float>();

    [TabGroup("Weight System Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("가중치 시스템 설정 검증 결과")]
    public string WeightSystemValidation => _maxUpgradeCollection?.ValidateConfiguration() ?? "설정되지 않음";

    [TabGroup("Weight System Debug")]
    [ShowInInspector, ReadOnly]
    public WeightBasedUpgrade? LastSelectedUpgrade { get; private set; }

    [TabGroup("Hybrid System Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("하이브리드 난이도 시스템 설정 검증 결과")]
    public string HybridSystemValidation => _hybridDifficulty.ValidateConfiguration();

    [TabGroup("Hybrid System Debug")]
    [ShowInInspector, ReadOnly]
    [InfoBox("설정된 적 타입 목록")]
    public PrefabType[] ConfiguredEnemyTypes => _hybridDifficulty.GetConfiguredEnemyTypes();
    #endregion

    #region Private Fields
    private Dictionary<PrefabType, SpawnStatRange> _currentStatRanges;
    private int _currentMinPackSize;
    private int _currentMaxPackSize;
    private float _nextSpawnTime;
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

        CurrentCycle = 1;
        SpawnCount = 0;
        _nextSpawnTime = Time.time + _spawnIntervalSeconds;

        Debug.Log($"[EnemySpawner] Initialized with {_spawnPoints.Length} spawn points", this);
    }

    private void OnEnable()
    {
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

        SpawnImmediate(); // 즉시 스폰
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

        // 가중치 시스템 상태 초기화
        LastSelectedUpgrade = null;

        // 하이브리드 시스템 검증
        if (_hybridDifficulty.enemyTypeConfigs != null)
        {
            string validationResult = _hybridDifficulty.ValidateConfiguration();
            if (validationResult != "설정이 올바릅니다.")
            {
                Debug.LogWarning($"[EnemySpawner] Hybrid system validation after reset: {validationResult}", this);
            }
            else
            {
                PrefabType[] configuredTypes = _hybridDifficulty.GetConfiguredEnemyTypes();
                Debug.Log($"[EnemySpawner] Reset complete - Configured enemy types: {string.Join(", ", configuredTypes)}", this);
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Hybrid difficulty system is not configured", this);
        }

        // 가중치 시스템 검증
        if (_maxUpgradeCollection != null)
        {
            string validationResult = _maxUpgradeCollection.ValidateConfiguration();
            if (validationResult == "설정이 올바릅니다.")
            {
                var initialProbabilities = _maxUpgradeCollection.GetSelectionProbabilities(CurrentCycle);
                string probabilityInfo = string.Join(", ",
                    initialProbabilities.Select(kvp => $"{kvp.Key.GetDisplayName()}: {kvp.Value:F1}%"));
                Debug.Log($"[EnemySpawner] Initial upgrade probabilities: {probabilityInfo}", this);
            }
        }

        Debug.Log("[EnemySpawner] Difficulty reset to initial values", this);
        OnDifficultyUpgraded?.Invoke(CurrentCycle);
    }

    /// <summary>
    /// 특정 적 타입의 스탯 범위 설정
    /// </summary>
    /// <param name="enemyType">적 타입</param>
    /// <param name="statType">스탯 타입</param>
    /// <param name="minValue">최소값</param>
    /// <param name="maxValue">최대값</param>
    public void SetStatRange(PrefabType enemyType, SpawnStatType statType, float minValue, float maxValue)
    {
        if (!_currentStatRanges.ContainsKey(enemyType))
        {
            Debug.LogWarning($"[EnemySpawner] Enemy type {enemyType} not found in stat ranges", this);
            return;
        }

        // 값 검증
        minValue = Mathf.Max(0f, minValue);
        maxValue = Mathf.Max(minValue, maxValue);

        // 현재 스탯 범위 업데이트
        SpawnStatRange currentRange = _currentStatRanges[enemyType];
        currentRange.SetMinValue(statType, minValue);
        currentRange.SetMaxValue(statType, maxValue);
        _currentStatRanges[enemyType] = currentRange;

        Debug.Log($"[EnemySpawner] {enemyType} {statType} range set to [{minValue:F1}, {maxValue:F1}]", this);
    }

    /// <summary>
    /// 수동 난이도 업그레이드
    /// </summary>
    public void UpgradeDifficulty()
    {
        CurrentCycle++;

        // 필수 업그레이드 적용
        ApplyHybridUpgrades();

        // 가중치 기반 최대값 업그레이드 처리
        ProcessWeightBasedMaxUpgrade();

        Debug.Log($"[EnemySpawner] Manual difficulty upgrade to cycle {CurrentCycle}", this);
        OnDifficultyUpgraded?.Invoke(CurrentCycle);
    }
    #endregion

    #region Private Methods - Spawn Logic
    private PrefabType SelectRandomEnemyType()
    {
        if (_enemyConfigs == null || _enemyConfigs.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No enemy configs available. Using default EnemyNormal", this);
            return PrefabType.EnemyNormal;
        }

        // 총 가중치 계산
        float totalWeight = 0f;
        foreach (var config in _enemyConfigs)
        {
            totalWeight += config.GetCurrentWeight(CurrentCycle);
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[EnemySpawner] Total weight is zero. Using first config", this);
            return _enemyConfigs[0].enemyType;
        }

        // 가중치 기반 선택
        float randomPoint = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var config in _enemyConfigs)
        {
            currentSum += config.GetCurrentWeight(CurrentCycle);
            if (randomPoint <= currentSum)
            {
                return config.enemyType;
            }
        }

        // 안전장치
        return _enemyConfigs[_enemyConfigs.Length - 1].enemyType;
    }
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

        // 타입별 스폰 개수 추적
        //Dictionary<PrefabType, int> spawnedByType = new Dictionary<PrefabType, int>();
        if(LastSpawnedEnemy == null)
        {
            LastSpawnedEnemy = new Dictionary<PrefabType, int>();
        }
        LastSpawnedEnemy.Clear();
        Dictionary<PrefabType, int> spawnedByType = LastSpawnedEnemy;

        // 각 스폰 포인트에서 적 생성
        int actualSpawned = 0;
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (distribution[i] > 0)
            {

                SpawnEnemiesAtPointWithTracking(_spawnPoints[i], distribution[i], ref spawnedByType);
                actualSpawned += distribution[i];
            }
        }

        // 스폰 카운트 업데이트
        SpawnCount++;

        // 로그 및 이벤트 호출
        LogSpawnInfo(actualSpawned, distribution);
        OnSpawnCompleted?.Invoke(spawnedByType, CurrentCycle);

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
            var selectedType = SelectRandomEnemyType();
            ISpawnable enemy = CreateEnemyFromPool(spawnPosition, spawnRotation, selectedType);
            if (enemy != null)
            {
                // 스폰 완료 처리
                enemy.OnSpawned(this);
            }
        }
    }

    private ISpawnable CreateEnemyFromPool(Vector3 spawnPosition, Quaternion spawnRotation, PrefabType selectedType)
    {
        //// 확률 기반 적 타입 선택
        //selectedType = SelectRandomEnemyType();

        GameObject enemyObject = _enemyPool.SpawnObject(selectedType, spawnPosition, spawnRotation, isForcely: false);

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

        // 랜덤 스탯 생성 및 적용
        Dictionary<SpawnStatType, float> randomStats = GenerateRandomStats(selectedType);
        spawnable.ApplySpawnStats(randomStats);

        // 풀 반환 콜백 등록
        spawnable.RegisterPoolReturnCallback(returnedSpawnable =>
        {
            _enemyPool.ReturnObject(returnedSpawnable.GameObject);
        });

        return spawnable;
    }

    private void SpawnEnemiesAtPointWithTracking(Transform spawnPoint, int enemyCount, ref Dictionary<PrefabType, int> spawnedTypes)
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
            var selectedType = SelectRandomEnemyType();
            ISpawnable enemy = CreateEnemyFromPool(spawnPosition, spawnRotation, selectedType);
            if (enemy != null)
            {
                // 스폰 완료 처리
                enemy.OnSpawned(this);

                // 타입별 카운팅
                if (spawnedTypes.ContainsKey(selectedType))
                    spawnedTypes[selectedType]++;
                else
                    spawnedTypes[selectedType] = 1;
            }
        }
    }

    #endregion

    #region Private Methods - Difficulty Logic
    private void ProcessWeightBasedMaxUpgrade()
    {
        if (_maxUpgradeCollection == null)
        {
            Debug.LogWarning("[EnemySpawner] Max upgrade collection is null", this);
            return;
        }

        // 가중치 시스템 검증
        string validationResult = _maxUpgradeCollection.ValidateConfiguration();
        if (validationResult != "설정이 올바릅니다.")
        {
            Debug.LogError($"[EnemySpawner] Weight system validation failed: {validationResult}", this);
            return;
        }

        // 가중치 기반으로 하나의 업그레이드 선택
        WeightBasedUpgrade? selectedUpgrade = _maxUpgradeCollection.SelectUpgrade(CurrentCycle);

        if (selectedUpgrade.HasValue)
        {
            ApplyWeightBasedUpgrade(selectedUpgrade.Value);
            LastSelectedUpgrade = selectedUpgrade.Value;
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] No upgrade selected from weight system", this);
            LastSelectedUpgrade = null;
        }
    }

    private void CheckDifficultyUpgrade()
    {
        if (SpawnCount % _difficultyUpdateInterval == 0)
        {
            CurrentCycle++;

            // 필수 업그레이드 적용
            ApplyHybridUpgrades();

            // 가중치 기반 최대값 업그레이드 처리
            ProcessWeightBasedMaxUpgrade();

            Debug.Log($"[EnemySpawner] Auto difficulty upgrade to cycle {CurrentCycle}", this);
            OnDifficultyUpgraded?.Invoke(CurrentCycle);
        }
    }

    private void ApplyHybridUpgrades()
    {
        // 하이브리드 시스템 유효성 검사
        string validationResult = _hybridDifficulty.ValidateConfiguration();
        if (validationResult != "설정이 올바릅니다.")
        {
            Debug.LogWarning($"[EnemySpawner] Hybrid system validation failed: {validationResult}", this);
            return;
        }

        // 팩 사이즈 업그레이드 적용
        ApplyPackSizeHybridUpgrades();

        // 적 타입별 스탯 업그레이드 적용
        ApplyStatHybridUpgrades();

        Debug.Log($"[EnemySpawner] Hybrid upgrades applied for cycle {CurrentCycle}", this);
    }

    /// <summary>
    /// 팩 사이즈 하이브리드 업그레이드 적용
    /// </summary>
    private void ApplyPackSizeHybridUpgrades()
    {
        PrefabType[] configuredTypes = _hybridDifficulty.GetConfiguredEnemyTypes();

        foreach (var enemyType in configuredTypes)
        {
            var config = _hybridDifficulty.GetConfigForEnemyType(enemyType);
            if (!config.HasValue) continue;

            // 해당 타입의 팩 사이즈 영향도만큼 전체 팩 사이즈 증가
            float packSizeIncrease = config.Value.ApplyPackSizeIncrease(1f) - 1f; // 증가분만 계산

            if (packSizeIncrease > 0f)
            {
                _currentMinPackSize += Mathf.RoundToInt(packSizeIncrease);
                _currentMaxPackSize += Mathf.RoundToInt(packSizeIncrease);

                Debug.Log($"[EnemySpawner] {enemyType} pack size upgrade applied: +{packSizeIncrease:F1}", this);
            }
        }

        // 최소값이 최대값보다 커지지 않도록 보정
        _currentMinPackSize = Mathf.Max(_minPackSize, _currentMinPackSize);
        _currentMaxPackSize = Mathf.Max(_currentMinPackSize, _currentMaxPackSize);
    }

    /// <summary>
    /// 스탯 하이브리드 업그레이드 적용
    /// </summary>
    private void ApplyStatHybridUpgrades()
    {
        var enemyTypes = new List<PrefabType>(_currentStatRanges.Keys);

        foreach (var enemyType in enemyTypes)
        {
            var config = _hybridDifficulty.GetConfigForEnemyType(enemyType);
            if (!config.HasValue) continue;

            SpawnStatRange currentRange = _currentStatRanges[enemyType];

            // 각 스탯 타입별로 Min/Max 값 업그레이드
            foreach (SpawnStatType statType in System.Enum.GetValues(typeof(SpawnStatType)))
            {
                float currentMin = currentRange.GetMinValue(statType);
                float currentMax = currentRange.GetMaxValue(statType);

                // 하이브리드 증가 적용
                float newMin = config.Value.statModifiers.ApplyMinIncrease(statType, currentMin);
                float newMax = config.Value.statModifiers.ApplyMaxIncrease(statType, currentMax);

                // 최대값이 최소값보다 작아지지 않도록 보정
                newMax = Mathf.Max(newMin, newMax);

                currentRange.SetMinValue(statType, newMin);
                currentRange.SetMaxValue(statType, newMax);

                Debug.Log($"[EnemySpawner] {enemyType} {statType} hybrid upgrade: " +
                         $"Min {currentMin:F1}→{newMin:F1}, Max {currentMax:F1}→{newMax:F1}", this);
            }

            _currentStatRanges[enemyType] = currentRange;
        }
    }

    private void ApplyWeightBasedUpgrade(WeightBasedUpgrade upgrade)
    {
        if (upgrade.IsPackSizeUpgrade)
        {
            // 팩 사이즈 최대값 업그레이드
            _currentMaxPackSize += Mathf.RoundToInt(upgrade.upgradeAmount);

            Debug.Log($"[EnemySpawner] Pack size max upgraded by {upgrade.upgradeAmount:F1} to {_currentMaxPackSize} " +
                     $"(Weight: {upgrade.GetCurrentWeight(CurrentCycle):F1})", this);
        }
        else
        {
            // 모든 적 타입의 스탯 최대값 업그레이드
            SpawnStatType statType = upgrade.GetStatType();
            var enemyTypes = new List<PrefabType>(_currentStatRanges.Keys);

            foreach (var enemyType in enemyTypes)
            {
                SpawnStatRange currentRange = _currentStatRanges[enemyType];
                float currentMax = currentRange.GetMaxValue(statType);
                float newMax = currentMax + upgrade.upgradeAmount;
                currentRange.SetMaxValue(statType, newMax);
                _currentStatRanges[enemyType] = currentRange;
            }

            Debug.Log($"[EnemySpawner] {statType} max upgraded by {upgrade.upgradeAmount:F1} for all enemy types " +
                     $"(Weight: {upgrade.GetCurrentWeight(CurrentCycle):F1})", this);
        }

        // 선택 확률 정보 로깅
        var probabilities = _maxUpgradeCollection.GetSelectionProbabilities(CurrentCycle);
        string probabilityInfo = string.Join(", ",
            probabilities.Select(kvp => $"{kvp.Key.GetDisplayName()}: {kvp.Value:F1}%"));

        Debug.Log($"[EnemySpawner] Upgrade selection probabilities - {probabilityInfo}", this);
    }
    #endregion

    #region Private Methods - Stat Generation
    private Dictionary<SpawnStatType, float> GenerateRandomStats(PrefabType enemyType)
    {
        Dictionary<SpawnStatType, float> randomStats = new Dictionary<SpawnStatType, float>();

        if (!_currentStatRanges.ContainsKey(enemyType))
        {
            Debug.LogWarning($"[EnemySpawner] No stat range found for {enemyType}. Using default values.", this);
            return randomStats;
        }

        SpawnStatRange statRange = _currentStatRanges[enemyType];

        // 각 스탯 타입별로 랜덤 값 생성
        foreach (SpawnStatType statType in System.Enum.GetValues(typeof(SpawnStatType)))
        {
            float randomValue = statRange.GetRandomValue(statType);
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
        if (_enemyConfigs == null || _enemyConfigs.Length == 0)
        {
            Debug.LogError("[EnemySpawner] Enemy configs not set!", this);
            return;
        }

        if (_currentStatRanges == null)
        {
            _currentStatRanges = new Dictionary<PrefabType, SpawnStatRange>();
        }
        else
        {
            _currentStatRanges.Clear();
        }

        // 각 적 타입별로 현재 스탯 범위 초기화
        foreach (var config in _enemyConfigs)
        {
            _currentStatRanges[config.enemyType] = config.statRange;
        }

        // 현재 팩 사이즈 범위 초기화
        _currentMinPackSize = _minPackSize;
        _currentMaxPackSize = _maxPackSize;

        Debug.Log($"[EnemySpawner] Stat ranges initialized for {_currentStatRanges.Count} enemy types", this);
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