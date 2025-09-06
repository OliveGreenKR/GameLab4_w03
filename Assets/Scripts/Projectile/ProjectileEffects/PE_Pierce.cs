using UnityEngine;

using System.Collections.Generic;

/// <summary>
/// 투사체의 관통력 추가, 관통할 때마다 데미지 감소시킴.
/// 중복 추가 시 관통력만 추가하고, 실제로 부착되지는 않음(데미지 감소 중복 획득 방지)
/// </summary>
public class PierceEffect : IProjectileEffect
{
    #region Properties
    public int Priority => 1; // 낮은 우선순위 (먼저 실행)
    #endregion

    #region Private Fields
    private int _pierceCount = 1;
    #endregion

    #region Constructor
    public PierceEffect(int pierceCount = 1)
    {
        _pierceCount = pierceCount;
    }
    #endregion

    #region IProjectileEffect Implementation
    public bool OnAttachedProjectile(ProjectileBase owner, List<IProjectileEffect> existingEffects)
    {
        // 항상 체력 증가 (관통력 누적)
        owner.ModifyStat(BattleStatType.Health, _pierceCount);

        // 기존 PE 존재시 새로운 PE는 리스트에 추가 안 함
        for (int i = 0; i < existingEffects.Count; i++)
        {
            if (existingEffects[i] is PierceEffect)
            {
                return false; // 리스트 추가 거부
            }
        }

        return true; // 최초 PE만 리스트 추가
    }

    public void OnHit(ProjectileBase owner, Collider target)
    {
        // 단일 PE만 존재하므로 정확히 1회만 호출됨
        float currentAttack = owner.GetStat(BattleStatType.Attack);
        owner.SetStat(BattleStatType.Attack, currentAttack * 0.4f);
    }

    public void OnDestroy(ProjectileBase owner)
    {
        // 소멸 시 특별한 처리 없음
    }

    public void OnUpdate(ProjectileBase owner)
    {
        // 매 프레임 처리 없음
    }
    #endregion

    #region Battle Stat Modifier
    private void OnAttached()
    {
        Debug.Log("PierceAttached!");
    }
    #endregion

}