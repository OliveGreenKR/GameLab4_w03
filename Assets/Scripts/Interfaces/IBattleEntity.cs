using UnityEngine;
/// <summary>
/// 배틀에 참여할 수 있는 모든 객체가 구현해야 하는 인터페이스
/// </summary>
public interface IBattleEntity
{
    #region Properties
    /// <summary>
    /// 배틀 엔티티의 Transform
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// 배틀 엔티티의 GameObject
    /// </summary>
    GameObject GameObject { get; }

    /// <summary>
    /// 현재 생존 상태
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// 팀 식별자 (같은 팀끼리는 공격하지 않음)
    /// </summary>
    int TeamId { get; }
    #endregion

    #region Battle Methods
    /// <summary>
    /// 다른 배틀 엔티티로부터 데미지를 받음
    /// </summary>
    /// <param name="attacker">공격자</param>
    /// <param name="damage">기본 데미지 값</param>
    /// <returns>실제 적용된 데미지</returns>
    float TakeDamage(IBattleEntity attacker, float damage);

    /// <summary>
    /// 다른 배틀 엔티티에게 데미지를 가함
    /// </summary>
    /// <param name="target">대상</param>
    /// <param name="baseDamage">기본 데미지 값</param>
    /// <returns>실제 가해진 데미지</returns>
    float DealDamage(IBattleEntity target, float baseDamage);

    /// <summary>
    /// 엔티티가 죽었을 때 호출
    /// </summary>
    /// <param name="killer">킬러 (null 가능)</param>
    void OnDeath(IBattleEntity killer = null);
    #endregion

    #region Stat Access
    /// <summary>
    /// 특정 스탯 값 조회
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>스탯 값</returns>
    float GetStat(BattleStatType statType);

    /// <summary>
    /// 특정 스탯 값 수정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 값</param>
    void SetStat(BattleStatType statType, float value);

    /// <summary>
    /// 특정 스탯에 값 추가/감소
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="delta">변화량</param>
    void ModifyStat(BattleStatType statType, float delta);
    #endregion
}

/// <summary>
/// 배틀 스탯 타입 정의
/// </summary>
public enum BattleStatType
{
    Health,         // 체력 (수치)
    MaxHealth,      // 최대 체력 (수치)
    Attack,         // 공격력 (수치)
    AttackSpeed,    // 공격속도 (수치)
    EffectRange,    // 효과범위 (배수)
}