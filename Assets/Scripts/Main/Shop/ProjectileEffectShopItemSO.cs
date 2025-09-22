using Sirenix.OdinInspector;
using System.Text;
using UnityEngine;

/// <summary>
/// 투사체 효과 상점 아이템
/// ProjectileEffectSO를 추가하는 아이템에 사용
/// </summary>
[CreateAssetMenu(fileName = "ProjectileEffectItem_", menuName = "Shop System/Projectile Effect Item", order = 3)]
public class ProjectileEffectShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Effect")]
    [Header("Projectile Effect")]
    [Required]
    [SerializeField] private ProjectileEffectSO _projectileEffect;
    #endregion

    #region Properties - Effect Data
    public ProjectileEffectSO ProjectileEffect => _projectileEffect;
    #endregion

    #region Protected Methods - Description Generation Override
    /// <summary>투사체 효과 상점 아이템 특화 설명 생성</summary>
    /// <returns>투사체 효과 아이템 맞춤 설명</returns>
    protected override string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 기본 아이템 정보
        description.AppendLine($"[{GetItemTypeDisplayName(ItemType)}]");

        // 투사체 효과 정보
        if (_projectileEffect != null)
        {
            // 효과 타입별 특화 정보만 표시
            string effectInfo = GetProjectileEffectTypeInfo(_projectileEffect);
            if (!string.IsNullOrEmpty(effectInfo))
            {
                description.AppendLine(effectInfo);
            }

            // 우선순위 정보
            string priorityLevel = GetProjectileEffectPriorityDescription(_projectileEffect.Priority);
            description.AppendLine($"우선순위: {_projectileEffect.Priority} ({priorityLevel})");
        }
        else
        {
            description.AppendLine("효과: 투사체 효과 미할당");
        }

        // 가격 정보
        description.Append($"가격: {BasePrice}G");

        if (PriceInflationMultiplier > 1.0f)
        {
            description.Append($" (구매시 {PriceInflationMultiplier:F1}배씩 증가)");
        }

        return description.ToString().Trim();
    }

    /// <summary>투사체 효과 우선순위 레벨 설명 반환</summary>
    /// <param name="priority">우선순위 값</param>
    /// <returns>우선순위 설명</returns>
    private string GetProjectileEffectPriorityDescription(int priority)
    {
        if (priority <= 20) return "최우선";
        if (priority <= 40) return "높음";
        if (priority <= 60) return "보통";
        if (priority <= 80) return "낮음";
        return "최후순";
    }

    /// <summary>투사체 효과 타입별 특화 정보 반환</summary>
    /// <param name="effect">투사체 효과</param>
    /// <returns>타입별 정보 문자열</returns>
    private string GetProjectileEffectTypeInfo(ProjectileEffectSO effect)
    {
        switch (effect)
        {
            case SplitEffectSO splitEffect:
                var splitInfo = new StringBuilder();
                splitInfo.AppendLine($"분열: {splitEffect.SplitProjectileCount}개 (+{splitEffect.SplitAngleRange:F0}°)");
                if (splitEffect.SpeedMultiplier != 1.0f)
                    splitInfo.AppendLine($"속도: {FormatMultiplierChange(splitEffect.SpeedMultiplier)}");
                if (splitEffect.DamageMultiplier != 1.0f)
                    splitInfo.AppendLine($"데미지: {FormatMultiplierChange(splitEffect.DamageMultiplier)}");
                if (splitEffect.LifetimeMultiplier != 1.0f)
                    splitInfo.AppendLine($"생존시간: {FormatMultiplierChange(splitEffect.LifetimeMultiplier)}");
                return splitInfo.ToString().TrimEnd();

            case PiercingEffectSO piercingEffect:
                string pierceText = piercingEffect.AdditionalPierceCount > 0 ? $"+{piercingEffect.AdditionalPierceCount} 관통" : "관통 추가 없음";
                if (!Mathf.Approximately(piercingEffect.DamageMultiplier, 1.0f))
                {
                    string damageEffect = piercingEffect.DamageMultiplier > 1.0f ?
                        $"관통 후 데미지 증가 {piercingEffect.DamageMultiplier:F1}배" :
                        $"관통 후 데미지 감소 {piercingEffect.DamageMultiplier:F1}배";
                    return $"{pierceText}\n({damageEffect})";
                }
                return pierceText;

            default:
                // 기본 EffectDescription 사용
                if (!string.IsNullOrEmpty(effect.EffectDescription))
                {
                    return effect.EffectDescription.Replace("\n", " ").Trim();
                }
                return $"{effect.name} 효과";
        }
    }

    /// <summary>배율 변화를 사용자 친화적 텍스트로 변환</summary>
    /// <param name="multiplier">배율 값</param>
    /// <returns>변화 설명 텍스트</returns>
    private string FormatMultiplierChange(float multiplier)
    {
        if (multiplier > 1.0f)
        {
            return $"+{(multiplier - 1.0f) * 100:F0}%";
        }
        else if (multiplier < 1.0f)
        {
            return $"{(multiplier - 1.0f) * 100:F0}%";
        }
        else
        {
            return "변화없음";
        }
    }
    #endregion
}