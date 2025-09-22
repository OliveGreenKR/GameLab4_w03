using Sirenix.OdinInspector;
using UnityEngine;

public enum PierceApplicationMode
{
    SetAbsolute,    // 절대값으로 설정 (기존 값 무시)
    AddToExisting   // 기존 값에 추가

}
/// <summary>
/// 투사체 관통 이펙트 ScriptableObject
/// 관통 횟수 추가와 관통시 데미지 배수 조정 기능 제공
/// </summary>
[CreateAssetMenu(fileName = "New Piercing Effect", menuName = "Projectile Effects/Piercing Effect")]
public class PiercingEffectSO : ProjectileEffectSO
{
    #region Serialized Fields
    [TabGroup("Piercing Settings")]
    [Header("Pierce Application Mode")]
    [InfoBox("관통력 적용 방식 선택 -  절댓값 적용, 기존값에 추가")]
    [SerializeField] private PierceApplicationMode _applicationMode = PierceApplicationMode.AddToExisting;

    [TabGroup("Piercing Settings")]
    [Header("Pierce Count")]
    [InfoBox("관통력 수치입니다")]
    [PropertyRange(0, 10)]
    [SuffixLabel("hits")]
    [SerializeField] private int _pierceCount = 1;

    [TabGroup("Piercing Settings")]
    [Header("Damage Multiplier")]
    [InfoBox("관통시 데미지 배수\n1.0 = 변화없음, 0.5 = 50% 감소, 1.5 = 50% 증가")]
    [PropertyRange(0.0f, 10.0f)]
    [SuffixLabel("x")]
    [SerializeField] private float _pierceCountDamageMultiplier = 1.0f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int AdditionalPierceCount => _pierceCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DamageMultiplier => _pierceCountDamageMultiplier;
    #endregion

    #region ProjectileEffectSO Implementation
    /// <summary>
    /// 투사체에 관통 이펙트를 적용합니다
    /// </summary>
    /// <param name="projectile">효과를 적용할 투사체</param>
    public override void AttachToProjectile(IProjectile projectile)
    {
        if (!ValidateProjectile(projectile))
            return;

        if (_pierceCount >= 0)
        {
            if (_applicationMode == PierceApplicationMode.SetAbsolute)
            {
                projectile.SetPierceCount(_pierceCount);
            }
            else
            {
                // AddToExisting
                projectile.ModifyPierceCount(_pierceCount);
            }
        }

        // 충돌 이벤트 구독 (데미지 배수는 충돌할 때마다 적용)
        if (!Mathf.Approximately(_pierceCountDamageMultiplier, 1.0f))
        {
            projectile.AfterProjectileHit += AfterProjectileHit;
        }
    }
    /// <summary>
    /// 투사체 소멸시 정리 작업
    /// </summary>
    /// <param name="projectile">정리할 투사체</param>
    public override void DetachFromProjectile(IProjectile projectile)
    {
        if (ValidateProjectile(projectile))
        {
            projectile.AfterProjectileHit -= AfterProjectileHit;
        }
        LogEffect("Detached from projectile (no cleanup needed)", projectile);
    }

    private void AfterProjectileHit(IProjectile projectile, Collider hitCollider)
    {
        // 매 충돌이후 데미지 배수 적용
        projectile.ModifyDamageMultiplier(_pierceCountDamageMultiplier);
        LogEffect($"Applied pierce damage multiplier: {_pierceCountDamageMultiplier:F2}x. New total: {projectile.DamageMultiplier:F2}x", projectile);
    }

  
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        // 값 범위 검증
        _pierceCount = Mathf.Clamp(_pierceCount, 0, 10);
        _pierceCount = Mathf.Clamp(_pierceCount, 0, 10);
        _pierceCountDamageMultiplier = Mathf.Clamp(_pierceCountDamageMultiplier, 0.1f, 3.0f);

        // 설명 자동 업데이트
        UpdateDescription();
    }
    #endregion

    #region Private Methods
    private void UpdateDescription()
    {
        string pierceText;
        if (_applicationMode == PierceApplicationMode.SetAbsolute)
        {
            pierceText = _pierceCount > 0 ? $"관통 {_pierceCount}회" : "관통 없음";
        }
        else
        {
            pierceText = _pierceCount > 0 ? $"+{_pierceCount} 관통" : "관통 추가 없음";
        }

        string damageText = _pierceCountDamageMultiplier != 1.0f ?
            $", 데미지 {_pierceCountDamageMultiplier:F1}배" : "";

        _description = $"{pierceText}{damageText}";
    }
    #endregion
}