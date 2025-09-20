/// <summary>
/// 무기 스탯을 수정하는 효과 인터페이스
/// </summary>
public interface IWeaponEffect
{
    #region Properties
    /// <summary>
    /// 효과 적용 우선순위 (낮을수록 먼저 적용)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 효과 이름
    /// </summary>
    string EffectName { get; }
    #endregion

    #region Methods
    /// <summary>
    /// 무기 스탯에 효과를 적용합니다
    /// </summary>
    /// <param name="baseStats">원본 무기 스탯</param>
    /// <returns>효과가 적용된 새로운 스탯</returns>
    WeaponStatData ApplyToWeapon(WeaponStatData baseStats);

    /// <summary>
    /// 효과 적용 가능 여부 검증
    /// </summary>
    /// <param name="weaponStats">검증할 무기 스탯</param>
    /// <returns>적용 가능하면 true</returns>
    bool CanApplyToWeapon(WeaponStatData weaponStats);
    #endregion
}