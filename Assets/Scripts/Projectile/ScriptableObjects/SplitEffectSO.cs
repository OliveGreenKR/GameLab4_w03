using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum SplitApplicationMode
{
    SetAbsolute,    // 절대값으로 설정
    AddToExisting   // 기존 값에 추가
}

/// <summary>
/// 투사체 분열 이펙트 ScriptableObject
/// 분열 속성들을 설정하며 실제 분열은 투사체가 처리
/// </summary>
[CreateAssetMenu(fileName = "New Split Effect", menuName = "Projectile Effects/Split Effect")]
public class SplitEffectSO : ProjectileEffectSO
{
    #region Serialized Fields
    [TabGroup("Split Settings")]
    [Header("Split Application Mode")]
    [InfoBox("분열 횟수 적용 방식")]
    [SerializeField] private SplitApplicationMode _applicationMode = SplitApplicationMode.AddToExisting;

    [TabGroup("Split Settings")]
    [Header("Split Count")]
    [InfoBox("분열할 투사체 개수")]
    [PropertyRange(1, 8)]
    [SuffixLabel("projectiles")]
    [SerializeField] private int _splitCount = 3;

    [TabGroup("Split Settings")]
    [Header("Split Angle Range")]
    [InfoBox("분열 각도 범위 (Yaw)")]
    [PropertyRange(30f, 360f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _splitAngleRangeDegrees = 120f;

    [TabGroup("Projectile Modifiers")]
    [Header("Speed Multiplier")]
    [InfoBox("분열 부착 시 투사체 속도 배율\n1.0 = 변화없음, 0.5 = 50% 감소, 1.5 = 50% 증가")]
    [PropertyRange(0.1f, 3.0f)]
    [SuffixLabel("x")]
    [SerializeField] private float _speedMultiplier = 1.0f;

    [TabGroup("Projectile Modifiers")]
    [Header("Damage Multiplier")]
    [InfoBox("분열 시 데미지 배율\n1.0 = 변화없음, 0.5 = 50% 감소, 1.5 = 50% 증가")]
    [PropertyRange(0.1f, 3.0f)]
    [SuffixLabel("x")]
    [SerializeField] private float _damageMultiplier = 1.0f;

    [TabGroup("Projectile Modifiers")]
    [Header("Lifetime Multiplier")]
    [InfoBox("분열 부착 시 생명시간 배율\n1.0 = 변화없음, 0.5 = 50% 감소, 1.5 = 50% 증가")]
    [PropertyRange(0.1f, 3.0f)]
    [SuffixLabel("x")]
    [SerializeField] private float _lifetimeMultiplier = 1.0f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int SplitProjectileCount => _splitCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float SplitAngleRange => _splitAngleRangeDegrees;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public SplitApplicationMode ApplicationMode => _applicationMode;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float SpeedMultiplier => _speedMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DamageMultiplier => _damageMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float LifetimeMultiplier => _lifetimeMultiplier;
    #endregion

    #region ProjectileEffectSO Implementation
    public override void AttachToProjectile(IProjectile projectile)
    {
        if (!ValidateProjectile(projectile))
        {
            Debug.Log("nonvalidate projectile :Split");
            return;
        }

        // 분열 가능 횟수 설정
        if (_applicationMode == SplitApplicationMode.SetAbsolute)
        {
            projectile.SetSplitAvailableCount(1);
        }
        else
        {
            projectile.ModifySplitAvailableCount(1);
        }

        // 분열 속성 설정
        projectile.ModifySplitProjectileCount(_splitCount);
        Debug.Log($"Befroe attach split effect_ range: {projectile.SplitAngleRange}");
        projectile.ModifySplitAngleRange(_splitAngleRangeDegrees);
        Debug.Log($"attach split effect_ range to {projectile.SplitAngleRange}");

        // 투사체 속성 배율 적용 (부착 시 즉시 적용)
        if (!Mathf.Approximately(_speedMultiplier, 1.0f))
        {
            projectile.ModifySpeedMultiplier(_speedMultiplier);
        }

        if (!Mathf.Approximately(_damageMultiplier, 1.0f))
        {
            projectile.ModifyDamageMultiplier(_damageMultiplier);
        }

        if (!Mathf.Approximately(_lifetimeMultiplier, 1.0f))
        {
            float currentLifetime = projectile.RemainingLifetime;
            projectile.SetLifetime(currentLifetime * _lifetimeMultiplier);
        }

        //LogEffect($"Attached split effect. Available: {projectile.SplitAvailableCount}, Count: {projectile.SplitProjectileCount}, Angle: {projectile.SplitAngleRange}", projectile);
    }

    public override void DetachFromProjectile(IProjectile projectile)
    {
        //LogEffect("Detached from projectile", projectile);

    }
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        _splitCount = Mathf.Clamp(_splitCount, 1, 8);
        _splitAngleRangeDegrees = Mathf.Clamp(_splitAngleRangeDegrees, 30f, 360f);
        _speedMultiplier = Mathf.Clamp(_speedMultiplier, 0.1f, 3.0f);
        _damageMultiplier = Mathf.Clamp(_damageMultiplier, 0.1f, 3.0f);
        _lifetimeMultiplier = Mathf.Clamp(_lifetimeMultiplier, 0.1f, 3.0f);
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        string modeText = _applicationMode == SplitApplicationMode.SetAbsolute ? "절대" : "추가";
        string baseText = $"{modeText} {_splitCount}분열, 각도 {_splitAngleRangeDegrees:F0}°";

        string modifierText = "";

        if (!Mathf.Approximately(_speedMultiplier, 1.0f))
        {
            modifierText += $", 속도 {_speedMultiplier:F1}x";
        }

        if (!Mathf.Approximately(_damageMultiplier, 1.0f))
        {
            modifierText += $", 데미지 {_damageMultiplier:F1}x";
        }

        if (!Mathf.Approximately(_lifetimeMultiplier, 1.0f))
        {
            modifierText += $", 생명시간 {_lifetimeMultiplier:F1}x";
        }

        _description = baseText + modifierText;
    }
    #endregion

    #region Protected Methods - Description Generation Override
    /// <summary>분열 이펙트 특화 설명 생성</summary>
    /// <returns>분열 이펙트 맞춤 설명</returns>
    protected override string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 이펙트 기본 정보
        description.AppendLine($"[분열 이펙트]");

        // 분열 설정 정보
        string applicationModeText = _applicationMode == SplitApplicationMode.SetAbsolute ? "절대값 설정" : "기존값 추가";
        description.AppendLine($"분열 방식: {applicationModeText}");
        description.AppendLine($"분열 개수: {_splitCount}개");
        description.AppendLine($"분열 각도: {_splitAngleRangeDegrees:F0}°");

        // 투사체 수정 정보
        var modifiers = new List<string>();

        if (_speedMultiplier != 1.0f)
        {
            string speedChange = _speedMultiplier > 1.0f ? $"+{(_speedMultiplier - 1.0f) * 100:F0}%" : $"{(_speedMultiplier - 1.0f) * 100:F0}%";
            modifiers.Add($"속도 {speedChange}");
        }

        if (_damageMultiplier != 1.0f)
        {
            string damageChange = _damageMultiplier > 1.0f ? $"+{(_damageMultiplier - 1.0f) * 100:F0}%" : $"{(_damageMultiplier - 1.0f) * 100:F0}%";
            modifiers.Add($"데미지 {damageChange}");
        }

        if (_lifetimeMultiplier != 1.0f)
        {
            string lifetimeChange = _lifetimeMultiplier > 1.0f ? $"+{(_lifetimeMultiplier - 1.0f) * 100:F0}%" : $"{(_lifetimeMultiplier - 1.0f) * 100:F0}%";
            modifiers.Add($"생존시간 {lifetimeChange}");
        }

        if (modifiers.Count > 0)
        {
            description.AppendLine($"효과: {string.Join(", ", modifiers)}");
        }

        // 우선순위 정보
        string priorityLevel = GetPriorityLevelDescription(_priority);
        description.Append($"우선순위: {_priority} ({priorityLevel})");

        return description.ToString().Trim();
    }
    #endregion
}