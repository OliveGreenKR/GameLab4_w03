using Sirenix.OdinInspector;
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
    [PropertyRange(2, 8)]
    [SuffixLabel("projectiles")]
    [SerializeField] private int _splitCount = 3;

    [TabGroup("Split Settings")]
    [Header("Split Angle Range")]
    [InfoBox("분열 각도 범위 (Yaw)")]
    [PropertyRange(30f, 360f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _splitAngleRangeDegrees = 120f;
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
        projectile.SetSplitAngleRange(_splitAngleRangeDegrees);

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

        _splitCount = Mathf.Clamp(_splitCount, 2, 8);
        _splitAngleRangeDegrees = Mathf.Clamp(_splitAngleRangeDegrees, 30f, 360f);
        UpdateDescription();
    }

    private void UpdateDescription()
    {
        string modeText = _applicationMode == SplitApplicationMode.SetAbsolute ? "절대" : "추가";
        _description = $"{modeText} {_splitCount}분열, 각도 {_splitAngleRangeDegrees:F0}°";
    }
    #endregion
}