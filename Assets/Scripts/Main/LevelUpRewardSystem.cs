using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Collections;  // IEnumerator용
using TMPro;              // TMP_Text용
using UnityEngine;

/// <summary>
/// 보상 타입 열거형
/// </summary>
public enum RewardType
{
    StatIncrease,           // 스탯 증가
    ProjectileEffect,       // 투사체 이펙트
    RareProjectileEffect,   // 레어 투사체 이펙트
    RareStatIncrease        // 레어 스탯 증가
}

/// <summary>
/// 스탯 증가 보상 데이터
/// </summary>
[Serializable]
public struct StatReward
{
    [InfoBox("스탯 증가 보상의 이름과 데이터")]
    public string rewardName;
    [Required]
    public BattleStatData statData;
}

/// <summary>
/// 투사체 이펙트 보상 데이터
/// </summary>
[Serializable]
public struct ProjectileEffectReward
{
    [InfoBox("투사체 이펙트 보상의 이름과 데이터")]
    public string rewardName;
    [Required]
    public ProjectileEffectSO effectAsset;
}

/// <summary>
/// 레어 투사체 이펙트 보상 데이터
/// </summary>
[Serializable]
public struct RareProjectileEffectReward
{
    [InfoBox("레어 투사체 이펙트 보상의 이름과 데이터")]
    public string rewardName;
    [Required]
    public ProjectileEffectSO effectAsset;
}

/// <summary>
/// 레어 스탯 증가 보상 데이터
/// </summary>
[Serializable]
public struct RareStatReward
{
    [InfoBox("레어 스탯 증가 보상의 이름과 데이터")]
    public string rewardName;
    [Required]
    public BattleStatData statData;
}

/// <summary>
/// 레벨업 보상 시스템
/// </summary>
public class LevelUpRewardSystem : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("UI")]
    [Header("Reward Display UI")]
    [Required]
    [SerializeField] private TMP_Text _rewardDisplayText;

    [TabGroup("UI")]
    [SuffixLabel("seconds")]
    [PropertyRange(0.5f, 10f)]
    [SerializeField] private float _displayDurationSeconds = 3f;

    [TabGroup("References")]
    [Header("Target References")]
    [Required]
    [SerializeField] private ProjectileLauncher _projectileLauncher;

    [TabGroup("References")]
    [Required]
    [SerializeField] private BattleStatComponent _playerBattleStat;

    [TabGroup("Rewards", "Normal")]
    [Header("Normal Stat Rewards (50%)")]
    [InfoBox("일반 스탯 증가 보상 리스트")]
    [SerializeField] private List<StatReward> _statRewards = new List<StatReward>();

    [TabGroup("Rewards", "Normal")]
    [Header("Normal Projectile Effect Rewards (30%)")]
    [InfoBox("일반 투사체 이펙트 보상 리스트")]
    [SerializeField] private List<ProjectileEffectReward> _projectileEffectRewards = new List<ProjectileEffectReward>();

    [TabGroup("Rewards", "Rare")]
    [Header("Rare Projectile Effect Rewards (10%)")]
    [InfoBox("레어 투사체 이펙트 보상 리스트")]
    [SerializeField] private List<RareProjectileEffectReward> _rareProjectileEffectRewards = new List<RareProjectileEffectReward>();

    [TabGroup("Rewards", "Rare")]
    [Header("Rare Stat Rewards (10%)")]
    [InfoBox("레어 스탯 증가 보상 리스트")]
    [SerializeField] private List<RareStatReward> _rareStatRewards = new List<RareStatReward>();

    [TabGroup("Settings")]
    [Header("Reward Probabilities")]
    [InfoBox("각 보상 타입별 확률 가중치")]
    [PropertyRange(1, 100)]
    [SerializeField] private int _statIncreaseWeight = 50;

    [TabGroup("Settings")]
    [PropertyRange(1, 100)]
    [SerializeField] private int _projectileEffectWeight = 30;

    [TabGroup("Settings")]
    [PropertyRange(1, 100)]
    [SerializeField] private int _rareProjectileEffectWeight = 10;

    [TabGroup("Settings")]
    [PropertyRange(1, 100)]
    [SerializeField] private int _rareStatIncreaseWeight = 10;
    #endregion

    #region Events
    /// <summary>
    /// 보상이 적용되었을 때 발생하는 이벤트
    /// </summary>
    public event Action<RewardType, string> OnRewardApplied; // (보상타입, 보상이름)
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int TotalWeight => _statIncreaseWeight + _projectileEffectWeight + _rareProjectileEffectWeight + _rareStatIncreaseWeight;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsSystemReady => ValidateReferences() && ValidateRewardLists();

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Dictionary<RewardType, float> RewardProbabilities => CalculateRewardProbabilities();

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public string LastAppliedReward { get; private set; } = "None";
    #endregion

    #region Serialized Fuction for Debug
    [TabGroup("Debug")]
    [Button(ButtonSizes.Large)]
    private void EarnReward() => TriggerReward();

    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLevelUpEvents();
    }
    #endregion

    #region Private Fields
    // UI Display
    private Coroutine _displayCoroutine;
    #endregion

    #region Public Methods - System Control
    /// <summary>
    /// 보상 시스템 초기화 및 이벤트 구독
    /// </summary>
    public void Initialize()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[LevelUpRewardSystem] References validation failed!", this);
            return;
        }

        if (!ValidateRewardLists())
        {
            Debug.LogError("[LevelUpRewardSystem] Reward lists validation failed!", this);
            return;
        }

        SubscribeToLevelUpEvents();
        Debug.Log("[LevelUpRewardSystem] System initialized successfully", this);
    }

    /// <summary>
    /// 수동으로 보상 트리거 (테스트용)
    /// </summary>
    public void TriggerReward()
    {
        if (!IsSystemReady)
        {
            Debug.LogWarning("[LevelUpRewardSystem] System not ready for reward trigger", this);
            return;
        }

        RewardType selectedType = SelectRewardType();
        ApplyReward(selectedType);
    }
    #endregion

    #region Private Methods - Event Handling
    private void SubscribeToLevelUpEvents()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LevelUpRewardSystem] GameManager.Instance is null!", this);
            return;
        }

        GameManager.Instance.OnPlayerLevelUp -= OnPlayerLevelUp;
        GameManager.Instance.OnPlayerLevelUp += OnPlayerLevelUp;
        Debug.Log("[LevelUpRewardSystem] Subscribed to GameManager level up events", this);
    }

    private void UnsubscribeFromLevelUpEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerLevelUp -= OnPlayerLevelUp;
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"[LevelUpRewardSystem] Player reached level {newLevel}! Selecting reward...", this);

        RewardType selectedType = SelectRewardType();
        ApplyReward(selectedType);
    }
    #endregion

    #region Private Methods - Reward Selection
    private RewardType SelectRewardType()
    {
        int totalWeight = TotalWeight;
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        // 스탯 증가 (50%)
        currentWeight += _statIncreaseWeight;
        if (randomValue < currentWeight)
            return RewardType.StatIncrease;

        // 투사체 이펙트 (30%)
        currentWeight += _projectileEffectWeight;
        if (randomValue < currentWeight)
            return RewardType.ProjectileEffect;

        // 레어 투사체 이펙트 (10%)
        currentWeight += _rareProjectileEffectWeight;
        if (randomValue < currentWeight)
            return RewardType.RareProjectileEffect;

        // 레어 스탯 증가 (10%)
        return RewardType.RareStatIncrease;
    }

    private void ApplyReward(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.StatIncrease:
                ApplyStatReward();
                break;
            case RewardType.ProjectileEffect:
                ApplyProjectileEffectReward();
                break;
            case RewardType.RareProjectileEffect:
                ApplyRareProjectileEffectReward();
                break;
            case RewardType.RareStatIncrease:
                ApplyRareStatReward();
                break;
        }
    }

    private void ApplyStatReward()
    {
        if (_statRewards.Count == 0)
        {
            Debug.LogWarning("[LevelUpRewardSystem] No stat rewards available!", this);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _statRewards.Count);
        StatReward selectedReward = _statRewards[randomIndex];

        if (selectedReward.statData != null && _playerBattleStat != null)
        {
            // 스탯 데이터 적용 (기존 스탯에 추가)
            _playerBattleStat.ModifyStat(BattleStatType.Health, selectedReward.statData.BaseHealth);
            _playerBattleStat.ModifyStat(BattleStatType.Attack, selectedReward.statData.BaseAttack);
            _playerBattleStat.ModifyStat(BattleStatType.AttackSpeed, selectedReward.statData.BaseAttackSpeed);

            string rewardText = $"{GetRewardTypeDisplayName(RewardType.StatIncrease)}: {selectedReward.rewardName}";
            LastAppliedReward = rewardText;

            OnRewardApplied?.Invoke(RewardType.StatIncrease, selectedReward.rewardName);
            Debug.Log($"[LevelUpRewardSystem] Applied {rewardText}", this);

            ShowRewardText(rewardText);
        }
    }

    private void ApplyProjectileEffectReward()
    {
        if (_projectileEffectRewards.Count == 0)
        {
            Debug.LogWarning("[LevelUpRewardSystem] No projectile effect rewards available!", this);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _projectileEffectRewards.Count);
        ProjectileEffectReward selectedReward = _projectileEffectRewards[randomIndex];

        if (selectedReward.effectAsset != null && _projectileLauncher != null)
        {
            _projectileLauncher.AddEffect(selectedReward.effectAsset);

            string rewardText = $"{GetRewardTypeDisplayName(RewardType.ProjectileEffect)}: {selectedReward.rewardName}";
            LastAppliedReward = rewardText;

            OnRewardApplied?.Invoke(RewardType.ProjectileEffect, selectedReward.rewardName);
            Debug.Log($"[LevelUpRewardSystem] Applied {rewardText}", this);

            ShowRewardText(rewardText);
        }
    }

    private void ApplyRareProjectileEffectReward()
    {
        if (_rareProjectileEffectRewards.Count == 0)
        {
            Debug.LogWarning("[LevelUpRewardSystem] No rare projectile effect rewards available!", this);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _rareProjectileEffectRewards.Count);
        RareProjectileEffectReward selectedReward = _rareProjectileEffectRewards[randomIndex];

        if (selectedReward.effectAsset != null && _projectileLauncher != null)
        {
            _projectileLauncher.AddEffect(selectedReward.effectAsset);

            string rewardText = $"{GetRewardTypeDisplayName(RewardType.RareProjectileEffect)}: {selectedReward.rewardName}";
            LastAppliedReward = rewardText;

            OnRewardApplied?.Invoke(RewardType.RareProjectileEffect, selectedReward.rewardName);
            Debug.Log($"[LevelUpRewardSystem] Applied {rewardText}", this);

            ShowRewardText(rewardText);
        }
    }

    private void ApplyRareStatReward()
    {
        if (_rareStatRewards.Count == 0)
        {
            Debug.LogWarning("[LevelUpRewardSystem] No rare stat rewards available!", this);
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _rareStatRewards.Count);
        RareStatReward selectedReward = _rareStatRewards[randomIndex];

        if (selectedReward.statData != null && _playerBattleStat != null)
        {
            // 레어 스탯은 더 큰 증가량 적용
            _playerBattleStat.ModifyStat(BattleStatType.Health, selectedReward.statData.BaseHealth);
            _playerBattleStat.ModifyStat(BattleStatType.Attack, selectedReward.statData.BaseAttack);
            _playerBattleStat.ModifyStat(BattleStatType.AttackSpeed, selectedReward.statData.BaseAttackSpeed);

            string rewardText = $"{GetRewardTypeDisplayName(RewardType.RareStatIncrease)}: {selectedReward.rewardName}";
            LastAppliedReward = rewardText;

            OnRewardApplied?.Invoke(RewardType.RareStatIncrease, selectedReward.rewardName);
            Debug.Log($"[LevelUpRewardSystem] Applied {rewardText}", this);

            ShowRewardText(rewardText);
        }
    }
    #endregion

    #region Private Methods - Utility
    private bool ValidateReferences()
    {
        if (_projectileLauncher == null)
        {
            Debug.LogError("[LevelUpRewardSystem] ProjectileLauncher reference is missing!", this);
            return false;
        }

        if (_playerBattleStat == null)
        {
            Debug.LogError("[LevelUpRewardSystem] Player BattleStatComponent reference is missing!", this);
            return false;
        }

        return true;
    }

    private bool ValidateRewardLists()
    {
        bool hasValidRewards = false;

        if (_statRewards.Count > 0)
        {
            hasValidRewards = true;
            Debug.Log($"[LevelUpRewardSystem] Stat rewards loaded: {_statRewards.Count}", this);
        }

        if (_projectileEffectRewards.Count > 0)
        {
            hasValidRewards = true;
            Debug.Log($"[LevelUpRewardSystem] Projectile effect rewards loaded: {_projectileEffectRewards.Count}", this);
        }

        if (_rareProjectileEffectRewards.Count > 0)
        {
            hasValidRewards = true;
            Debug.Log($"[LevelUpRewardSystem] Rare projectile effect rewards loaded: {_rareProjectileEffectRewards.Count}", this);
        }

        if (_rareStatRewards.Count > 0)
        {
            hasValidRewards = true;
            Debug.Log($"[LevelUpRewardSystem] Rare stat rewards loaded: {_rareStatRewards.Count}", this);
        }

        if (!hasValidRewards)
        {
            Debug.LogWarning("[LevelUpRewardSystem] No rewards are configured in any list!", this);
        }

        return hasValidRewards;
    }

    private Dictionary<RewardType, float> CalculateRewardProbabilities()
    {
        Dictionary<RewardType, float> probabilities = new Dictionary<RewardType, float>();

        if (TotalWeight == 0) return probabilities;

        probabilities[RewardType.StatIncrease] = (_statIncreaseWeight / (float)TotalWeight) * 100f;
        probabilities[RewardType.ProjectileEffect] = (_projectileEffectWeight / (float)TotalWeight) * 100f;
        probabilities[RewardType.RareProjectileEffect] = (_rareProjectileEffectWeight / (float)TotalWeight) * 100f;
        probabilities[RewardType.RareStatIncrease] = (_rareStatIncreaseWeight / (float)TotalWeight) * 100f;

        return probabilities;
    }

    private string GetRewardTypeDisplayName(RewardType rewardType)
    {
        switch (rewardType)
        {
            case RewardType.StatIncrease:
                return "Stat";
            case RewardType.ProjectileEffect:
                return "Projectile";
            case RewardType.RareProjectileEffect:
                return "Rare Projectile";
            case RewardType.RareStatIncrease:
                return "Rare Stat";
            default:
                return "Unknown";
        }
    }
    #endregion

    #region Private Methods - UI Display
    private void ShowRewardText(string rewardText)
    {
        if (_rewardDisplayText == null) return;

        // 기존 코루틴이 실행 중이면 중단
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        // 새 코루틴 시작
        _displayCoroutine = StartCoroutine(DisplayTextCoroutine(rewardText));
    }

    private void HideRewardText()
    {
        if (_rewardDisplayText != null)
        {
            _rewardDisplayText.gameObject.SetActive(false);
        }

        _displayCoroutine = null;
    }

    private IEnumerator DisplayTextCoroutine(string text)
    {
        // 텍스트 설정 및 활성화
        _rewardDisplayText.text = text;
        _rewardDisplayText.gameObject.SetActive(true);

        // 설정된 시간만큼 대기
        yield return new WaitForSeconds(_displayDurationSeconds);

        // 텍스트 숨김
        HideRewardText();
    }
    #endregion
}