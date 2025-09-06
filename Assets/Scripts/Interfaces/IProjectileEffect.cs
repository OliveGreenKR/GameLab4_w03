using System.Collections.Generic;
using UnityEngine;

public interface IProjectileEffect
{
    /// <summary>
    /// 효과 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 투사체에 효과가 추가될 때 호출됩니다
    /// </summary>
    /// <param name="owner">소유 투사체</param>
    /// <param name="existingEffects">기존 효과 목록</param>
    /// <returns>추가 성공 여부 (false시 기존 효과 수정으로 처리)</returns>
    bool OnAttachedProjectile(ProjectileBase owner, List<IProjectileEffect> existingEffects);

    /// <summary>
    /// 투사체가 대상과 충돌했을 때 호출됩니다
    /// </summary>
    /// <param name="owner">소유 투사체</param>
    /// <param name="target">충돌 대상</param>
    void OnHit(ProjectileBase owner, Collider target);

    /// <summary>
    /// 투사체가 생명시간 종료로 소멸할 때 호출됩니다
    /// </summary>
    /// <param name="owner">소유 투사체</param>
    void OnDestroy(ProjectileBase owner);

    /// <summary>
    /// 매 프레임 호출됩니다 (추적 등 지속 효과용)
    /// </summary>
    /// <param name="owner">소유 투사체</param>
    void OnUpdate(ProjectileBase owner);
}