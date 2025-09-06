using System;
using UnityEngine;

/// <summary>
/// 배틀 엔티티 간의 상호작용을 처리하는 정적 시스템
/// </summary>
public static class BattleInteractionSystem
{
    #region Events
    /// <summary>
    /// 배틀 상호작용이 발생했을 때의 이벤트
    /// </summary>
    public static event Action<IBattleEntity, IBattleEntity, float> OnBattleInteraction;

    /// <summary>
    /// 엔티티가 죽었을 때의 이벤트
    /// </summary>
    public static event Action<IBattleEntity, IBattleEntity> OnEntityKilled;
    #endregion

    #region Public Methods
    /// <summary>
    /// 두 배틀 엔티티 간의 데미지 상호작용 처리
    /// </summary>
    /// <param name="attacker">공격자</param>
    /// <param name="target">대상</param>
    /// <param name="baseDamage">기본 데미지</param>
    /// <returns>실제 적용된 데미지</returns>
    public static float ProcessDamageInteraction(IBattleEntity attacker, IBattleEntity target, float baseDamage)
    {
        if (!ValidateEntities(attacker, target))
            return 0f;

        if (IsSameTeam(attacker, target))
            return 0f;

        if (!target.IsAlive)
            return 0f;

        float attackerAttack = attacker.GetStat(BattleStatType.Attack);
        float finalDamage = CalculateDamage(attackerAttack, baseDamage);

        float actualDamage = target.TakeDamage(attacker, finalDamage);

        TriggerBattleInteraction(attacker, target, actualDamage);

        if (!target.IsAlive)
        {
            TriggerEntityKilled(attacker, target);
        }

        return actualDamage;
    }

    /// <summary>
    /// 같은 팀인지 확인
    /// </summary>
    /// <param name="entity1">엔티티 1</param>
    /// <param name="entity2">엔티티 2</param>
    /// <returns>같은 팀이면 true</returns>
    public static bool IsSameTeam(IBattleEntity entity1, IBattleEntity entity2)
    {
        if (entity1 == null || entity2 == null)
            return false;

        return entity1.TeamId == entity2.TeamId;
    }

    /// <summary>
    /// 데미지 계산 (공격력 기반)
    /// </summary>
    /// <param name="attackerAttack">공격자 공격력</param>
    /// <param name="baseDamage">기본 데미지</param>
    /// <returns>최종 데미지</returns>
    public static float CalculateDamage(float attackerAttack, float baseDamage)
    {
        return baseDamage + (attackerAttack * 0.1f);
    }

    /// <summary>
    /// 효과 범위 적용
    /// </summary>
    /// <param name="baseRange">기본 범위</param>
    /// <param name="rangeMultiplier">범위 배수</param>
    /// <returns>적용된 범위</returns>
    public static float ApplyEffectRange(float baseRange, float rangeMultiplier)
    {
        return baseRange * Mathf.Max(0.1f, rangeMultiplier);
    }
    #endregion

    #region Private Methods
    private static void TriggerBattleInteraction(IBattleEntity attacker, IBattleEntity target, float damage)
    {
        OnBattleInteraction?.Invoke(attacker, target, damage);
    }

    private static void TriggerEntityKilled(IBattleEntity killer, IBattleEntity victim)
    {
        OnEntityKilled?.Invoke(killer, victim);
    }

    private static bool ValidateEntities(IBattleEntity attacker, IBattleEntity target)
    {
        if (attacker == null)
        {
            Debug.LogWarning("[BattleInteractionSystem] Attacker is null");
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning("[BattleInteractionSystem] Target is null");
            return false;
        }

        if (attacker.GameObject == null || target.GameObject == null)
        {
            Debug.LogWarning("[BattleInteractionSystem] Entity GameObject is null");
            return false;
        }

        return true;
    }
    #endregion
}