using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 플레이어 전용 배틀 엔티티 컴포넌트
/// </summary>
public class PlayerBattleEntity : BaseBattleEntity
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Player References")]
    [Required]
    [SerializeField] private NewPlayerController _playerController;

    [TabGroup("Settings")]
    [Header("Player Battle Settings")]
    [SuffixLabel("units")]
    [PropertyRange(1f, 1000f)]
    [SerializeField] private float _initialHealth = 100f;

    [TabGroup("Settings")]
    [Header("battleEntity Settings")]
    [SerializeField] private int _teamId = 0;

    [TabGroup("Collision")]
    [Header("Enemy Detection")]
    [SerializeField] private LayerMask _enemyLayerMask = -1;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsPlayerAlive => IsAlive && _playerController != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float HealthPercentage => GetCurrentStat(BattleStatType.MaxHealth) > 0
        ? GetCurrentStat(BattleStatType.Health) / GetCurrentStat(BattleStatType.MaxHealth)
        : 0f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public NewPlayerController PlayerController => _playerController;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        InitializeReferences();
    }

    protected override void Start()
    {
        base.Start();
        InitializePlayerStats();
        SetupPlayerTeam();
        SubscribeToEvents();

        Debug.Log($"[PlayerBattleEntity] Player initialized with {GetCurrentStat(BattleStatType.Health)}/{GetCurrentStat(BattleStatType.MaxHealth)} HP", this);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeFromEvents();
    }
    #endregion

    #region Public Methods - Player Specific
    /// <summary>
    /// 플레이어 초기화 (게임 시작 또는 리스폰시)
    /// </summary>
    public void InitializePlayer()
    {
        if (_battleStat == null) return;

        // 체력 완전 회복
        _battleStat.SetCurrentStat(BattleStatType.Health, GetCurrentStat(BattleStatType.MaxHealth));

        Debug.Log($"[PlayerBattleEntity] Player reinitialized to full health", this);
    }

    /// <summary>
    /// 플레이어 체력 회복
    /// </summary>
    /// <param name="amount">회복량</param>
    /// <returns>실제 회복된 양</returns>
    public float HealPlayer(float amount)
    {
        if (_battleStat == null || !IsAlive || amount <= 0f)
            return 0f;

        return _battleStat.Heal(amount);
    }

    /// <summary>
    /// 플레이어 최대 체력 설정
    /// </summary>
    /// <param name="maxHealth">최대 체력</param>
    public void SetMaxHealth(float maxHealth)
    {
        if (_battleStat == null || maxHealth <= 0f) return;

        float old = _battleStat.MaxHealth;
        float delta = maxHealth - old;
        _battleStat.SetCurrentStat(BattleStatType.MaxHealth, maxHealth);
        _battleStat.ModifyStat(BattleStatType.Health, delta);

        Debug.Log($"[PlayerBattleEntity] Max health set to {maxHealth}", this);
    }

    /// <summary>
    /// 플레이어 배틀 스탯 수정 
    /// </summary>
    /// <param name="statType">수정할 스탯 타입</param>
    /// <param name="value">추가할 값</param>
    public void ModifyBattleStat(BattleStatType statType, float value)
    {
        if (_battleStat == null || value == 0f) return;

        float currentValue = _battleStat.GetCurrentStat(statType);
        float newValue = currentValue + value;

        // 음수 방지 (체력 제외)
        if (statType != BattleStatType.Health && newValue < 0f)
            newValue = 0f;

        _battleStat.SetCurrentStat(statType, newValue);

        Debug.Log($"[PlayerBattleEntity] {statType} modified by {value:F1}, new value: {newValue:F1}", this);
    }

    /// <summary>
    /// 플레이어 배틀 스탯 배율 적용 (상점 업그레이드용)
    /// </summary>
    /// <param name="statType">수정할 스탯 타입</param>
    /// <param name="multiplier">적용할 배율</param>
    public void ModifyBattleStatMultiplier(BattleStatType statType, float multiplier)
    {
        if (_battleStat == null || multiplier <= 0f) return;

        float currentValue = _battleStat.GetCurrentStat(statType);
        float newValue = currentValue * multiplier;

        _battleStat.SetCurrentStat(statType, newValue);

        Debug.Log($"[PlayerBattleEntity] {statType} multiplied by {multiplier:F2}, new value: {newValue:F1}", this);
    }

    /// <summary>
    /// 정규화된 체력 비율 조회 (0.0 ~ 1.0)
    /// </summary>
    /// <returns>현재 체력 / 최대 체력</returns>
    public float GetHealthPercentage()
    {
        if (_battleStat == null) return 0f;

        float maxHealth = GetCurrentStat(BattleStatType.MaxHealth);
        if (maxHealth <= 0f) return 0f;

        float currentHealth = GetCurrentStat(BattleStatType.Health);
        return Mathf.Clamp01(currentHealth / maxHealth);
    }
    #endregion

    #region Protected Methods - Override
    /// <summary>
    /// 플레이어가 데미지를 받았을 때의 추가 처리
    /// </summary>
    /// <param name="damage">받은 데미지</param>
    /// <param name="attacker">공격자</param>
    protected override void OnDamageTakenFromBattleStat(float damage, IBattleEntity attacker)
    {
        base.OnDamageTakenFromBattleStat(damage, attacker);

        if (damage > 0f)
        {
            Debug.Log($"[PlayerBattleEntity] Player took {damage:F1} damage from {attacker?.GameObject.name ?? "Unknown"}. Health: {GetCurrentStat(BattleStatType.Health):F1}/{GetCurrentStat(BattleStatType.MaxHealth):F1}", this);

            // TODO: 플레이어 데미지 시각적/청각적 피드백 추가
            // TODO: 화면 흔들림, 데미지 UI 효과 등
        }
    }

    /// <summary>
    /// 플레이어 사망시 추가 처리
    /// </summary>
    /// <param name="killer">킬러</param>
    public override void OnDeath(IBattleEntity killer = null)
    {
        base.OnDeath(killer);

        Debug.Log($"[PlayerBattleEntity] Player has died. Killer: {killer?.GameObject.name ?? "Unknown"}", this);

        // 플레이어 입력 비활성화
        if (_playerController != null)
        {
            _playerController.DisableInput();
        }

        // TODO: GameManager에 게임오버 알림은 BattleStatComponent.OnDeath 이벤트로 처리
        // TODO: 사망 애니메이션, 이펙트 등 추가
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeReferences()
    {
        if (_playerController == null)
        {
            _playerController = GetComponent<NewPlayerController>();
        }

        if (_playerController == null)
        {
            Debug.LogError("[PlayerBattleEntity] NewPlayerController component required!", this);
        }

        if (_battleStat == null)
        {
            Debug.LogError("[PlayerBattleEntity] BattleStatComponent required!", this);
        }
    }

    private void InitializePlayerStats()
    {
        if (_battleStat == null) return;

        // 초기 체력 설정
        _battleStat.SetCurrentStat(BattleStatType.MaxHealth, _initialHealth);
        _battleStat.SetCurrentStat(BattleStatType.Health, _initialHealth);

        // 기본 공격력 설정 (플레이어는 무기로만 공격)
        _battleStat.SetCurrentStat(BattleStatType.Attack, 0f);
    }

    private void SetupPlayerTeam()
    {
        if (_battleStat == null) return;

        _battleStat.SetCurrentStat(BattleStatType.TeamId, _teamId);

        Debug.Log($"[PlayerBattleEntity] Player team set to {_teamId}", this);
    }
    #endregion

    #region Private Methods - Collision Detection
    private void HandleEnemyCollision(ControllerColliderHit hit)
    {
        IBattleEntity enemyEntity = hit.collider.GetComponent<IBattleEntity>();
        if (enemyEntity == null) return;

        // BattleInteractionSystem을 통해 데미지 처리
        float damage = enemyEntity.DealDamage(this, enemyEntity.GetCurrentStat(BattleStatType.Attack));

        if (damage > 0f)
        {
            Debug.Log($"[PlayerBattleEntity] Collision damage {damage:F1} from {hit.collider.name}", this);
        }
    }

    private void HandleEnemyCollision(Collider other)
    {
        IBattleEntity enemyEntity = other.GetComponent<IBattleEntity>();
        if (enemyEntity == null) return;

        // BattleInteractionSystem을 통해 데미지 처리
        float damage = enemyEntity.DealDamage(this, enemyEntity.GetCurrentStat(BattleStatType.Attack));

        if (damage > 0f)
        {
            Debug.Log($"[PlayerBattleEntity] Collision damage {damage:F1} from {other.name}", this);
        }
    }

    private bool IsValidEnemy(ControllerColliderHit hit)
    {
        // 레이어 마스크 확인
        if ((_enemyLayerMask.value & (1 << hit.collider.gameObject.layer)) == 0)
            return false;

        // 배틀 엔티티 컴포넌트 확인
        IBattleEntity enemyEntity = hit.collider.GetComponent<IBattleEntity>();
        if (enemyEntity == null || !enemyEntity.IsAlive)
            return false;

        // 다른 팀인지 확인
        if (BattleInteractionSystem.IsSameTeam(this, enemyEntity))
            return false;

        return true;
    }

    private bool IsValidEnemy(Collider other)
    {
        // 레이어 마스크 확인
        if ((_enemyLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return false;

        // 배틀 엔티티 컴포넌트 확인
        IBattleEntity enemyEntity = other.GetComponent<IBattleEntity>();
        if (enemyEntity == null || !enemyEntity.IsAlive)
            return false;

        // 다른 팀인지 확인
        if (BattleInteractionSystem.IsSameTeam(this, enemyEntity))
            return false;

        return true;
    }
    #endregion

    #region Private Methods - Event Handling
    private void SubscribeToEvents()
    {
        // BaseBattleEntity에서 이미 BattleStatComponent 이벤트 구독
        // 추가적인 플레이어 전용 이벤트 구독이 필요한 경우 여기에 추가
    }

    private void UnsubscribeFromEvents()
    {
        // BaseBattleEntity에서 이미 BattleStatComponent 이벤트 구독 해제
        // 추가적인 플레이어 전용 이벤트 구독 해제가 필요한 경우 여기에 추가
    }
    #endregion
}