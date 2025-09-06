using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

public class PierceEffect : IProjectileEffect
{
    #region Properties
    public int Priority => 1; // 낮은 우선순위 (먼저 실행)
    #endregion

    #region Private Fields
    private int _remainingPierceCount;
    #endregion

    #region Constructor
    public PierceEffect(int pierceCount)
    {
        _remainingPierceCount = pierceCount;
    }
    #endregion

    #region IProjectileEffect Implementation
    public bool OnAttachedProjectile(ProjectileBase owner, List<IProjectileEffect> existingEffects)
    {
        OnAttached();

        // 기존 관통 효과 찾기
        for (int i = 0; i < existingEffects.Count; i++)
        {
            if (existingEffects[i] is PierceEffect existingPierce)
            {
                // 기존 관통 개수에 추가
                existingPierce._remainingPierceCount += _remainingPierceCount;
                return false; // 새로운 효과 추가하지 않음
            }
        }

        return true; // 새로운 효과 추가
    }

    public void OnHit(ProjectileBase owner, Collider target)
    {
        _remainingPierceCount--;

        if (_remainingPierceCount <= 0)
        {
            // 관통 횟수 소진 시 투사체 소멸
            owner.DestroyProjectile();
        }
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