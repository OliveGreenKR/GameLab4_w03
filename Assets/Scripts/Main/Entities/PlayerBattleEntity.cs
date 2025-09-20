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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"[PlayerBattleEntity] OnControllerColliderHit with {hit.collider.name}", this);
        if (!IsAlive) return;

        if (IsValidEnemy(hit))
        {
            HandleEnemyCollision(hit);
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (!IsAlive) return;

    //    if (IsValidEnemy(other))
    //    {
    //        HandleEnemyCollision(other);
    //    }
    //}

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

        float currentHealthRatio = HealthPercentage;
        _battleStat.SetCurrentStat(BattleStatType.MaxHealth, maxHealth);

        // 현재 체력을 비율에 맞게 조정
        float newCurrentHealth = maxHealth * currentHealthRatio;
        _battleStat.SetCurrentStat(BattleStatType.Health, newCurrentHealth);

        Debug.Log($"[PlayerBattleEntity] Max health set to {maxHealth}, current: {newCurrentHealth:F1}", this);
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