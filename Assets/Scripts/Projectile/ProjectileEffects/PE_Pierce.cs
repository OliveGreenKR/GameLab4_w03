using UnityEngine;

/// <summary>
/// 투사체의 관통력 추가, 관통할 때마다 데미지 감소시킴.
/// 플레이어가 소유하며 투사체에 이벤트 구독 방식으로 효과 적용
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
    public void AttachToProjectile(ProjectileBase projectile)
    {
        // 관통력 증가 (체력 증가)
        projectile.ModifyStat(BattleStatType.Health, _pierceCount);

        // 관통시 데미지 감소 이벤트 구독
        projectile.OnProjectileHit += OnProjectileHit;

        Debug.Log($"[PierceEffect] Attached to {projectile.name} with {_pierceCount} pierce count");
    }

    public void DetachFromProjectile(ProjectileBase projectile)
    {
        // 이벤트 구독 해제
        projectile.OnProjectileHit -= OnProjectileHit;

        Debug.Log($"[PierceEffect] Detached from {projectile.name}");
    }
    #endregion

    #region Private Methods
    private void OnProjectileHit(ProjectileBase projectile, Collider target)
    {
        // 관통할 때마다 데미지 감소 (60% 유지)
        float currentAttack = projectile.GetStat(BattleStatType.Attack);
        projectile.SetStat(BattleStatType.Attack, currentAttack * 0.6f);

        Debug.Log($"[PierceEffect] Pierce damage reduced to {currentAttack * 0.6f}");
    }
    #endregion
}