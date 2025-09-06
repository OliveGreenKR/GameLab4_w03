using System.Collections.Generic;
using UnityEngine;

public interface IProjectileEffect
{
    /// <summary>
    /// 이펙트 우선순위 (낮을수록 먼저 적용)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 투사체에 이펙트를 적용합니다
    /// </summary>
    /// <param name="projectile">효과를 적용할 투사체</param>
    void AttachToProjectile(ProjectileBase projectile);

    /// <summary>
    /// 투사체 소멸시 정리 작업 (이벤트 구독 해제 등)
    /// </summary>
    /// <param name="projectile">정리할 투사체</param>
    void DetachFromProjectile(ProjectileBase projectile);
}