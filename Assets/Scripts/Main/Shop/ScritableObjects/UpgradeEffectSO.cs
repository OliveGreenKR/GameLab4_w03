using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 모든 업그레이드 효과의 기본 추상 클래스
/// </summary>
public abstract class UpgradeEffectSO : ScriptableObject
{
    #region Serialized Fields
    [TabGroup("Effect Info")]
    [Header("Basic Information")]
    [SerializeField] protected string _effectName = "New Upgrade Effect";

    [TabGroup("Effect Info")]
    [SerializeField, TextArea(2, 4)] protected string _effectDescription = "효과 설명을 입력하세요";

    [TabGroup("Effect Info")]
    [SerializeField] protected Sprite _effectIcon;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public string EffectName => _effectName;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public string EffectDescription => _effectDescription;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Sprite EffectIcon => _effectIcon;

    /// <summary>임시 버프 여부</summary>
    public virtual bool IsTemporary => false;

    /// <summary>버프 지속 시간 (초)</summary>
    public virtual float BuffDuration => 0f;
    #endregion

    #region Unity Lifecycle
    protected virtual void OnValidate() { }
    #endregion

    #region Public Methods - Abstract Interface
    /// <summary>업그레이드 효과를 적용합니다</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    public abstract void ApplyUpgrade(PlayerWeaponController weapon, PlayerBattleEntity player);

    /// <summary>업그레이드 적용 가능 여부를 확인합니다</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    /// <returns>적용 가능하면 true</returns>
    public abstract bool CanApply(PlayerWeaponController weapon, PlayerBattleEntity player);
    #endregion

    #region Public Methods - Virtual Interface
    /// <summary>업그레이드 효과를 제거합니다 (임시 버프용)</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    public virtual void RemoveUpgrade(PlayerWeaponController weapon, PlayerBattleEntity player) { }
    #endregion

    #region Protected Methods - Validation
    /// <summary>
    /// 업그레이드 대상의 유효성을 검사합니다
    /// </summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    /// <returns>유효하면 true</returns>
    protected bool ValidateTargets(PlayerWeaponController weapon, PlayerBattleEntity player)
    {
        if (weapon == null)
        {
            Debug.LogWarning($"[{GetType().Name}] PlayerWeaponController is null", this);
            return false;
        }

        if (player == null)
        {
            Debug.LogWarning($"[{GetType().Name}] PlayerBattleEntity is null", this);
            return false;
        }

        if (!player.IsAlive)
        {
            Debug.LogWarning($"[{GetType().Name}] Player is not alive", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 디버그 로그 출력 (에디터에서만)
    /// </summary>
    /// <param name="message">로그 메시지</param>
    protected void LogEffect(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[{GetType().Name}] {message}", this);
#endif
    }
    #endregion
}