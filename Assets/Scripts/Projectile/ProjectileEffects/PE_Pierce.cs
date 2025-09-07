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
    private float _damageReductionRatio = 0.6f;
    #endregion

    #region Constructor
    public PierceEffect(int pierceCount = 1, float damageReductionRatio = 0.6f)
    {
        _pierceCount = pierceCount;
        _damageReductionRatio = Mathf.Clamp01(damageReductionRatio);
    }
    #endregion

    #region IProjectileEffect Implementation
    public void AttachToProjectile(IProjectile projectile)
    {
        if (projectile == null)
        {
            Debug.LogWarning("[PierceEffect] Cannot attach to null projectile");
            return;
        }

        // 관통력 증가 (순수 투사체 시스템)
        projectile.ModifyPierceCount(_pierceCount);

        // 관통시 데미지 감소 이벤트 구독
        projectile.OnProjectileHit += OnProjectileHit;

        Debug.Log($"[PierceEffect] Attached to {projectile.GameObject.name} with {_pierceCount} pierce count");
    }

    public void DetachFromProjectile(IProjectile projectile)
    {
        if (projectile == null) return;

        // 이벤트 구독 해제
        projectile.OnProjectileHit -= OnProjectileHit;

        Debug.Log($"[PierceEffect] Detached from {projectile.GameObject.name}");
    }
    #endregion

    #region Private Methods
    private void OnProjectileHit(IProjectile projectile, Collider target)
    {
        // 관통할 때마다 데미지 배율 감소 (순수 투사체 시스템)
        projectile.ModifyDamageMultiplier(_damageReductionRatio);

        Debug.Log($"[PierceEffect] Pierce damage reduced by ratio {_damageReductionRatio}. Current multiplier: {projectile.DamageMultiplier}");
    }
    #endregion
}