using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 적 프리팹과 GameManager 간 통신을 중계하는 Proxy 컴포넌트
/// EnemySpawner와 GameManager의 독립성을 유지하면서 골드 보상 처리
/// </summary>
public class EnemyGameManagerProxy : MonoBehaviour
{
    #region Serialized Fields
    [Header("Enemy Type Info")]
    [InfoBox("이 적의 타입을 설정하세요. GameManager가 골드 보상 계산에 사용합니다.")]
    [SerializeField] private PrefabType _enemyType = PrefabType.EnemyNormal;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public PrefabType EnemyType => _enemyType;

    [TabGroup("Debug")]
    [Required, ShowInInspector, ReadOnly]
    public bool HasBattleEntity => _battleEntity != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsGameManagerAvailable => GameManager.Instance != null;
    #endregion

    #region Private Fields
    private IBattleEntity _battleEntity;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _battleEntity = GetComponent<IBattleEntity>();

        if (_battleEntity == null)
        {
            Debug.LogError("[EnemyGameManagerProxy] IBattleEntity component not found!", this);
        }
    }

    private void Start()
    {
        SubscribeToBattleEntity();
    }

    private void OnDestroy()
    {
        UnsubscribeFromBattleEntity();
    }
    #endregion

    #region Public Methods - Setup
    /// <summary>
    /// 적 타입 설정 (스폰 시 동적 설정용)
    /// </summary>
    /// <param name="enemyType">설정할 적 타입</param>
    public void SetEnemyType(PrefabType enemyType)
    {
        _enemyType = enemyType;
        Debug.Log($"[EnemyGameManagerProxy] Enemy type set to {enemyType}", this);
    }
    #endregion

    #region Private Methods - Event Handling
    private void OnBattleEntityDeath(IBattleEntity killer)
    {
        NotifyGameManagerOfDeath();
    }

    private void SubscribeToBattleEntity()
    {
        if (_battleEntity == null) return;

        // IBattleEntity는 OnDeath 이벤트가 없으므로 BaseBattleEntity의 OnDeath를 찾음
        if (_battleEntity is BaseBattleEntity battleEntity)
        {
            // BaseBattleEntity.OnDeath는 virtual 메서드이므로 직접 호출 불가
            // CharacterBattleEntity의 OnCharacterDeath 이벤트 사용
            if (_battleEntity is CharacterBattleEntity characterEntity)
            {
                characterEntity.OnCharacterDeath -= OnBattleEntityDeath;
                characterEntity.OnCharacterDeath += OnBattleEntityDeath;
            }
        }
    }

    private void UnsubscribeFromBattleEntity()
    {
        if (_battleEntity == null) return;

        if (_battleEntity is CharacterBattleEntity characterEntity)
        {
            characterEntity.OnCharacterDeath -= OnBattleEntityDeath;
        }
    }
    #endregion

    #region Private Methods - GameManager Communication
    private void NotifyGameManagerOfDeath()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EnemyGameManagerProxy] GameManager instance not found!", this);
            return;
        }

        // 현재 GameManager.OnEnemyKilled은 고정 보상 시스템
        // 향후 타입별 보상을 위해 타입 정보 로깅
        Debug.Log($"[EnemyGameManagerProxy] {_enemyType} enemy killed - notifying GameManager", this);

        // 기존 GameManager 메서드 호출 (killer는 null, victim은 자신)
        GameManager.Instance.OnEnemyKilled(null, _battleEntity);
    }
    #endregion
}