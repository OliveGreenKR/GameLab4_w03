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


}