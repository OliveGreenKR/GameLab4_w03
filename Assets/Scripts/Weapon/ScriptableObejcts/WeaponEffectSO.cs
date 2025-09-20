using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 무기 효과의 기본 ScriptableObject 추상 클래스
/// IWeaponEffect 인터페이스를 구현하여 기존 시스템과 호환
/// </summary>
public abstract class WeaponEffectSO : ScriptableObject, IWeaponEffect
{
    #region Serialized Fields
    [Header("Effect Settings")]
    [InfoBox("낮은 우선순위가 먼저 적용됩니다")]
    [PropertyRange(0, 100)]
    [SerializeField] protected int _priority = 50;

    [Header("Effect Info")]
    [SerializeField, TextArea(2, 4)]
    protected string _description = "효과 설명을 입력하세요";
    #endregion

    #region IWeaponEffect Implementation
    /// <summary>
    /// 효과 적용 우선순위 (낮을수록 먼저 적용)
    /// </summary>
    public int Priority => _priority;

    /// <summary>
    /// 효과 이름
    /// </summary>
    public string EffectName => name;

    /// <summary>
    /// 무기 스탯에 효과를 적용합니다
    /// </summary>
    /// <param name="baseStats">원본 무기 스탯</param>
    /// <returns>효과가 적용된 새로운 스탯</returns>
    public abstract WeaponStatData ApplyToWeapon(WeaponStatData baseStats);

    /// <summary>
    /// 효과 적용 가능 여부 검증
    /// </summary>
    /// <param name="weaponStats">검증할 무기 스탯</param>
    /// <returns>적용 가능하면 true</returns>
    public virtual bool CanApplyToWeapon(WeaponStatData weaponStats) { return true; }
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public string EffectDescription => _description;
    #endregion

    #region Unity Lifecycle
    protected virtual void OnValidate() { }
    #endregion

    #region Protected Methods - Utility
    protected void LogEffect(string message) { }
    protected bool ValidateWeaponStats(WeaponStatData stats) { return true; }
    #endregion
}